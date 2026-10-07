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

public sealed record ProspectCandidateDraft(string CompanyName, string Country, string Website, string Industry);

public sealed record DuplicateMatchResult(
    DuplicateTargetType TargetType,
    Guid TargetId,
    string TargetName,
    DuplicateMatchStrength Strength,
    decimal Similarity,
    IReadOnlyList<string> Reasons);

public sealed record ProspectConversionResult(ProspectCandidate Candidate, NetworkCompany NetworkCompany, ProspectConversion Conversion);
