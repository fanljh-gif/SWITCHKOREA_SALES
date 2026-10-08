namespace SalesManagement.Core.Models;
public sealed class InquiryAttachment : EntityBase { public Guid InquiryId { get; set; } public required string FileName { get; set; } public required string ContentType { get; set; } public long Size { get; set; } public Guid UploadedBy { get; set; } public DateTimeOffset UploadedAt { get; set; } public byte[] Content { get; set; } = []; }
