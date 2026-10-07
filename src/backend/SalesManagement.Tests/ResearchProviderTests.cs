using SalesManagement.Core.Models;
using SalesManagement.Core.Research;
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.Tests;

public sealed class ResearchProviderTests
{
    [Fact]
    public async Task Provider_Partial_Failure_Preserves_Completed_Candidates_And_Can_Retry()
    {
        var runs = new InMemoryResearchRunRepository(); var candidates = new InMemoryProspectCandidateRepository(); var evidence = new InMemoryResearchEvidenceRepository(); var duplicates = new InMemoryDuplicateMatchRepository();
        var provider = new SequenceProvider(failAfterFirst: true);
        var duplicateService = new DuplicateDetectionService(candidates, new InMemoryNetworkCompanyRepository(), new InMemoryCustomerRepository());
        var service = new ResearchRunService(runs, candidates, evidence, duplicates, provider, duplicateService);
        var profile = new ResearchTargetProfile { Name = "Global", Regions = ["한국"], Industries = ["전자"] };
        var run = await service.StartAsync(ResearchRunKind.Manual, profile);

        var partial = await service.ProcessAsync(run.Id, CancellationToken.None);

        Assert.Equal(ResearchRunStatus.Partial, partial.Status);
        Assert.Single(await candidates.ListAsync());
        provider.FailAfterFirst = false;
        var completed = await service.RetryFailedAsync(run.Id, CancellationToken.None);
        Assert.Equal(ResearchRunStatus.Completed, completed.Status);
        Assert.Equal(2, (await candidates.ListAsync()).Count);
    }

    [Fact]
    public async Task Same_Provider_Result_Id_Is_Idempotent()
    {
        var runs = new InMemoryResearchRunRepository(); var candidates = new InMemoryProspectCandidateRepository(); var evidence = new InMemoryResearchEvidenceRepository(); var duplicates = new InMemoryDuplicateMatchRepository();
        var provider = new SequenceProvider();
        var service = new ResearchRunService(runs, candidates, evidence, duplicates, provider, new DuplicateDetectionService(candidates, new InMemoryNetworkCompanyRepository(), new InMemoryCustomerRepository()));
        var profile = new ResearchTargetProfile { Name = "Global" };

        await service.ProcessAsync((await service.StartAsync(ResearchRunKind.Scheduled, profile)).Id, CancellationToken.None);
        await service.ProcessAsync((await service.StartAsync(ResearchRunKind.Scheduled, profile)).Id, CancellationToken.None);

        Assert.Equal(2, (await candidates.ListAsync()).Count);
    }

    private sealed class SequenceProvider(bool failAfterFirst = false) : IProspectResearchProvider
    {
        public bool FailAfterFirst { get; set; } = failAfterFirst;
        public async IAsyncEnumerable<ResearchProviderResult> SearchAsync(ResearchQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new("provider-1", "Hanul", "KR", "https://hanul.example", "자동차 전장", "Console modules", "https://hanul.example/products", new DateOnly(2026, 10, 8));
            await Task.Yield();
            if (FailAfterFirst) throw new HttpRequestException("provider timeout");
            yield return new("provider-2", "Vina", "VN", "https://vina.example", "가전", "Appliance modules", "https://vina.example/catalog", new DateOnly(2026, 10, 8));
        }
    }
}
