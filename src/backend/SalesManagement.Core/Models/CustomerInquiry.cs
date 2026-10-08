namespace SalesManagement.Core.Models;

public sealed class CustomerInquiry : EntityBase
{
    public required string InquiryNumber { get; set; }
    public required string Title { get; set; }
    public Guid CustomerId { get; set; }
    public required string CustomerName { get; set; }
    public required string ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public Guid OwnerUserId { get; set; }
    public DateOnly ReceivedAt { get; set; }
    public DateOnly ResponseDueAt { get; set; }
    public InquiryStatus Status { get; set; } = InquiryStatus.Received;
    public required string Summary { get; set; }
    public string? ResultMemo { get; set; }
    public List<InquiryActivity> Activities { get; set; } = [];
    public List<InquiryAttachment> Attachments { get; set; } = [];
}
