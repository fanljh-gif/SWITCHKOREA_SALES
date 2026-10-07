using SalesManagement.Core.Models;

namespace SalesManagement.Core.Research;

public sealed record ProspectScoringInput(
    int ProductFit,
    int IndustryFit,
    int SupplyChainFit,
    int RegionFit,
    int CompanyPotential,
    int EvidenceQuality);

public sealed record ProspectScoreResult(
    int ProductFit,
    int IndustryFit,
    int SupplyChainFit,
    int RegionFit,
    int CompanyPotential,
    int EvidenceQuality,
    int Total,
    ProspectPriority Priority,
    string RuleVersion);
