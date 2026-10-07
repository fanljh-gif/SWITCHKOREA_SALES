namespace SalesManagement.Core.Models;

public sealed class ResearchEvidence : EntityBase
{
    public Guid ProspectCandidateId { get; set; }
    public required string SourceUrl { get; set; }
    public required string Title { get; set; }
    public DateOnly ObservedDate { get; set; }
    public required string Summary { get; set; }
    public EvidenceConfidence Confidence { get; set; }
    public required string SourceHash { get; set; }
}
