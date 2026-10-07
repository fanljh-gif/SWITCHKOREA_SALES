namespace SalesManagement.Core.Models;

public sealed class ProspectDuplicateMatch : EntityBase
{
    public Guid ProspectCandidateId { get; set; }
    public DuplicateTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public required string TargetName { get; set; }
    public decimal Similarity { get; set; }
    public DuplicateMatchStrength Strength { get; set; }
    public List<string> Reasons { get; set; } = [];
    public DuplicateDecision Decision { get; set; } = DuplicateDecision.Pending;
}
