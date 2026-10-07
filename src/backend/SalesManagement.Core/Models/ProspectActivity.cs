namespace SalesManagement.Core.Models;

public sealed class ProspectActivity : EntityBase
{
    public Guid ProspectCandidateId { get; set; }
    public Guid ActorUserId { get; set; }
    public ProspectActivityAction Action { get; set; }
    public string? Note { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
