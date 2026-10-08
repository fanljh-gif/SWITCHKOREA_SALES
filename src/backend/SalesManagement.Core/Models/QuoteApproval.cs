namespace SalesManagement.Core.Models;
public sealed class QuoteApproval : EntityBase { public Guid RevisionId { get; set; } public Guid RequestedBy { get; set; } public DateTimeOffset RequestedAt { get; set; } public Guid? DecidedBy { get; set; } public DateTimeOffset? DecidedAt { get; set; } public QuoteApprovalDecision Decision { get; set; } public string? Reason { get; set; } }
