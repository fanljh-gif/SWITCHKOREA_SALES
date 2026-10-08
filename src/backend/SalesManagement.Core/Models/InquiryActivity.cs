namespace SalesManagement.Core.Models;
public sealed class InquiryActivity : EntityBase { public Guid InquiryId { get; set; } public InquiryActivityKind Kind { get; set; } public required string Body { get; set; } public Guid ActorId { get; set; } public DateTimeOffset OccurredAt { get; set; } }
