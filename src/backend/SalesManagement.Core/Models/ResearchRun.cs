namespace SalesManagement.Core.Models;

public sealed class ResearchRun : EntityBase
{
    public Guid ResearchTargetProfileId { get; set; }
    public ResearchRunKind Kind { get; set; }
    public ResearchRunStatus Status { get; set; } = ResearchRunStatus.Queued;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int DiscoveredCount { get; set; }
    public int EnrichedCount { get; set; }
    public int FailedCount { get; set; }
    public int ApiCallCount { get; set; }
    public decimal CostKrw { get; set; }
    public string? ErrorSummary { get; set; }
    public string? InputHash { get; set; }
}
