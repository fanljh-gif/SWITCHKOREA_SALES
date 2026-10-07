using SalesManagement.Core.Models;
using SalesManagement.Core.Research;
using SalesManagement.InMemory.Repositories;
using ClosedXML.Excel;

namespace SalesManagement.Tests;

public sealed class ProspectResearchTests
{
    [Theory]
    [InlineData(30, 20, 20, 10, 10, 10, 100, ProspectPriority.High)]
    [InlineData(20, 15, 15, 8, 6, 6, 70, ProspectPriority.High)]
    [InlineData(20, 15, 12, 8, 7, 7, 69, ProspectPriority.Normal)]
    [InlineData(15, 10, 8, 6, 5, 5, 49, ProspectPriority.Low)]
    public void Score_Uses_Fixed_Weights_And_Priority_Boundaries(
        int productFit, int industryFit, int supplyChainFit, int regionFit, int companyPotential, int evidenceQuality,
        int expectedTotal, ProspectPriority expectedPriority)
    {
        var result = new ProspectScoringService().Score(
            new ProspectScoringInput(productFit, industryFit, supplyChainFit, regionFit, companyPotential, evidenceQuality),
            "rules-2026.10");

        Assert.Equal(expectedTotal, result.Total);
        Assert.Equal(expectedPriority, result.Priority);
        Assert.Equal("rules-2026.10", result.RuleVersion);
        Assert.Equal(productFit, result.ProductFit);
    }

    [Theory]
    [InlineData(31, 0, 0, 0, 0, 0)]
    [InlineData(0, 21, 0, 0, 0, 0)]
    [InlineData(0, 0, 21, 0, 0, 0)]
    [InlineData(0, 0, 0, 11, 0, 0)]
    [InlineData(0, 0, 0, 0, 11, 0)]
    [InlineData(0, 0, 0, 0, 0, 11)]
    [InlineData(-1, 0, 0, 0, 0, 0)]
    public void Score_Rejects_Out_Of_Range_Inputs(int productFit, int industryFit, int supplyChainFit, int regionFit, int companyPotential, int evidenceQuality)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProspectScoringService().Score(
            new ProspectScoringInput(productFit, industryFit, supplyChainFit, regionFit, companyPotential, evidenceQuality),
            "rules-2026.10"));
    }

    [Fact]
    public void Mutable_Research_Entities_Inherit_EntityBase()
    {
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(ResearchTargetProfile)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(ResearchRun)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(ProspectCandidate)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(ResearchEvidence)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(ProspectAssessment)));
        Assert.True(typeof(EntityBase).IsAssignableFrom(typeof(ProspectActivity)));
    }

    [Fact]
    public async Task Duplicate_Detection_Uses_Root_Domain_And_Does_Not_Auto_Merge()
    {
        var candidates = new InMemoryProspectCandidateRepository();
        var existing = await candidates.AddAsync(Candidate("Existing GmbH", "DE", "example.com"));
        var service = new DuplicateDetectionService(candidates, new InMemoryNetworkCompanyRepository(), new InMemoryCustomerRepository());

        var matches = await service.FindMatchesAsync(new ProspectCandidateDraft("Other Legal Name", "DE", "https://www.example.com/products", "Industrial"));

        var match = Assert.Single(matches);
        Assert.Equal(existing.Id, match.TargetId);
        Assert.Equal(DuplicateMatchStrength.Strong, match.Strength);
        Assert.Equal(ProspectStatus.Collected, (await candidates.GetAsync(existing.Id))!.Status);
    }

    [Fact]
    public async Task Similar_Headquarters_And_Subsidiary_Name_Is_Review_Only()
    {
        var networks = new InMemoryNetworkCompanyRepository();
        var company = await networks.AddAsync(new NetworkCompany { CompanyName = "Nordlicht Controls Europe GmbH", Country = "DE", Industry = "가전", CurrentOwnerUserId = Guid.NewGuid() });
        var service = new DuplicateDetectionService(new InMemoryProspectCandidateRepository(), networks, new InMemoryCustomerRepository());

        var matches = await service.FindMatchesAsync(new ProspectCandidateDraft("Nordlicht Controls Europe AG", "DE", "https://nordlicht.example", "가전"));

        var match = Assert.Single(matches);
        Assert.Equal(company.Id, match.TargetId);
        Assert.Equal(DuplicateMatchStrength.Suspected, match.Strength);
    }

    private static ProspectCandidate Candidate(string name, string country, string domain, Guid? ownerId = null) => new()
    {
        ResearchRunId = Guid.NewGuid(), ProviderResultId = Guid.NewGuid().ToString(), CompanyName = name,
        NormalizedCompanyName = name.ToLowerInvariant(), Country = country, Region = country, Industry = "전자",
        Domain = domain, NormalizedDomain = domain, Website = $"https://{domain}", OwnerUserId = ownerId,
    };

    [Fact]
    public async Task Excel_Validation_And_Import_Are_Idempotent_By_File_Hash_And_Row()
    {
        var candidates = new InMemoryProspectCandidateRepository(); var evidence = new InMemoryResearchEvidenceRepository();
        var service = new ProspectExcelService(candidates, evidence, new DuplicateDetectionService(candidates, new InMemoryNetworkCompanyRepository(), new InMemoryCustomerRepository()));
        using var workbook = new XLWorkbook(); var sheet = workbook.AddWorksheet("Candidates");
        sheet.Cell(1, 1).Value = "CompanyName"; sheet.Cell(1, 2).Value = "Country"; sheet.Cell(1, 3).Value = "Website"; sheet.Cell(1, 4).Value = "Industry";
        sheet.Cell(2, 1).Value = "Pragati Meditech"; sheet.Cell(2, 2).Value = "IN"; sheet.Cell(2, 3).Value = "https://pragati.example"; sheet.Cell(2, 4).Value = "의료기기";
        using var stream = new MemoryStream(); workbook.SaveAs(stream); var bytes = stream.ToArray();

        var firstValidation = service.Validate(new MemoryStream(bytes));
        var secondValidation = service.Validate(new MemoryStream(bytes));
        var actor = new User { LoginId = "manager", PasswordHash = "hash", Name = "이재학", Email = "jhlee@switchkorea.com", Role = Role.Manager };
        await service.ImportAsync(firstValidation.ValidationId, actor);
        await service.ImportAsync(secondValidation.ValidationId, actor);

        Assert.Equal(firstValidation.ValidationId, secondValidation.ValidationId);
        Assert.Single(await candidates.ListAsync());
    }
}
