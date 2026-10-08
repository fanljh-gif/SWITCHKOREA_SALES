namespace SalesManagement.Core.Models;
public sealed class QuoteDispatch : EntityBase { public Guid RevisionId { get; set; } public DateTimeOffset SentAt { get; set; } public required string Memo { get; set; } public QuoteLanguage Language { get; set; } public Guid RecordedBy { get; set; } public string? PdfHash { get; set; } }
