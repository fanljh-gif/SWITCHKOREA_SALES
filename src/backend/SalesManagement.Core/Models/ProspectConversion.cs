namespace SalesManagement.Core.Models;

public sealed class ProspectConversion : EntityBase
{
    public Guid ProspectCandidateId { get; set; }
    public Guid NetworkCompanyId { get; set; }
    public Guid ApprovedByUserId { get; set; }
    public DateTimeOffset ConvertedAt { get; set; } = DateTimeOffset.UtcNow;
}
