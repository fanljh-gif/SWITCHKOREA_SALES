namespace SalesManagement.Core.Models;

public sealed class ProspectAssessment : EntityBase
{
    public Guid ProspectCandidateId { get; set; }
    public int ProductFit { get; set; }
    public int IndustryFit { get; set; }
    public int SupplyChainFit { get; set; }
    public int RegionFit { get; set; }
    public int CompanyPotential { get; set; }
    public int EvidenceQuality { get; set; }
    public int Total { get; set; }
    public ProspectPriority Priority { get; set; }
    public required string RuleVersion { get; set; }
    public required string AiSummary { get; set; }
    public required string AiRecommendation { get; set; }
    public List<string> AiRisks { get; set; } = [];
    public required string AiModel { get; set; }
    public DateTimeOffset AssessedAt { get; set; }
}
