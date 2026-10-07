using SalesManagement.Core.Models;

namespace SalesManagement.Core.Research;

public sealed class ProspectScoringService
{
    public ProspectScoreResult Score(ProspectScoringInput input, string ruleVersion)
    {
        if (string.IsNullOrWhiteSpace(ruleVersion)) throw new ArgumentException("Rule version is required.", nameof(ruleVersion));
        Validate(input.ProductFit, 30, nameof(input.ProductFit));
        Validate(input.IndustryFit, 20, nameof(input.IndustryFit));
        Validate(input.SupplyChainFit, 20, nameof(input.SupplyChainFit));
        Validate(input.RegionFit, 10, nameof(input.RegionFit));
        Validate(input.CompanyPotential, 10, nameof(input.CompanyPotential));
        Validate(input.EvidenceQuality, 10, nameof(input.EvidenceQuality));

        var total = input.ProductFit + input.IndustryFit + input.SupplyChainFit + input.RegionFit + input.CompanyPotential + input.EvidenceQuality;
        var priority = total >= 70 ? ProspectPriority.High : total >= 50 ? ProspectPriority.Normal : ProspectPriority.Low;
        return new(input.ProductFit, input.IndustryFit, input.SupplyChainFit, input.RegionFit, input.CompanyPotential, input.EvidenceQuality, total, priority, ruleVersion);
    }

    private static void Validate(int value, int maximum, string name)
    {
        if (value < 0 || value > maximum) throw new ArgumentOutOfRangeException(name, value, $"Score must be between 0 and {maximum}.");
    }
}
