using SalesManagement.Core.Models;
using SalesManagement.Core.Research;

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
}
