using SalesManagement.Core.Models;
namespace SalesManagement.Core.Quotations;
public sealed record QuoteLineInput(Guid ProductId, string Description, decimal Quantity, decimal UnitPrice);
public sealed record QuoteCalculationInput(Currency Currency, QuoteTaxType TaxType, decimal DiscountAmount, decimal ShippingAmount, decimal OtherAmount, IReadOnlyList<QuoteLineInput> Lines);
public sealed record QuoteAmounts(decimal SubtotalAmount, decimal SupplyAmount, decimal TaxAmount, decimal TotalAmount, IReadOnlyList<decimal> LineAmounts);
public sealed record SaveInquiryCommand(Guid? Id, string Title, Guid CustomerId, string ContactName, string? ContactEmail, Guid OwnerId, DateOnly ReceivedAt, DateOnly ResponseDueAt, string Summary, long ExpectedVersion = 0);
public sealed record InquiryQuery(string? Search = null, Guid? OwnerId = null, Guid? CustomerId = null, InquiryStatus? Status = null, bool OverdueOnly = false);
public sealed record AddInquiryActivityCommand(InquiryActivityKind Kind, string Body);
public sealed record AttachmentUpload(string FileName, string ContentType, byte[] Content);
public sealed record CreateQuoteCommand(Guid? InquiryId, Guid CustomerId, Guid OwnerId, QuoteTradeType TradeType, Currency Currency, QuoteTaxType TaxType, DateOnly ValidUntil, string? IncotermCode, string? NamedPlace, decimal DiscountAmount, decimal ShippingAmount, decimal OtherAmount, IReadOnlyList<QuoteLineInput> Lines, string? PaymentTerms = null, string? DeliveryTerms = null, string? Notes = null);
public sealed record SaveQuoteRevisionCommand(QuoteTradeType TradeType, Currency Currency, QuoteTaxType TaxType, DateOnly ValidUntil, string? IncotermCode, string? NamedPlace, decimal DiscountAmount, decimal ShippingAmount, decimal OtherAmount, IReadOnlyList<QuoteLineInput> Lines, long ExpectedVersion, string? PaymentTerms = null, string? DeliveryTerms = null, string? Notes = null);
public sealed record RecordDispatchCommand(DateTimeOffset SentAt, string Memo, QuoteLanguage Language, long ExpectedVersion);
public sealed record QuoteQuery(string? Search = null, Guid? OwnerId = null, Guid? CustomerId = null, QuoteStatus? Status = null, Currency? Currency = null);
public sealed record QuotationDashboardQuery(Guid? OwnerId = null);
public sealed record CurrencyAmount(Currency Currency, decimal Amount);
public sealed record QuotationDashboard(int UnansweredCount, int OverdueCount, int ApprovalPendingCount, int MonthlyQuoteCount, IReadOnlyList<CurrencyAmount> CurrencyAmounts, IReadOnlyDictionary<string, int> ResultCounts, decimal WinRatePercent);
public sealed record QuotePdfModel(string QuoteNumber, string RevisionLabel, string CustomerName, Currency Currency, decimal TotalAmount, string? IncotermCode, string? NamedPlace, QuoteLanguage Language, IReadOnlyList<QuoteLine> Lines)
{
    public string? PaymentTerms { get; init; }
    public string? DeliveryTerms { get; init; }
    public string? Notes { get; init; }
    public DateOnly? ValidUntil { get; init; }
    public QuoteTaxType TaxType { get; init; }
    public decimal SubtotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal ShippingAmount { get; init; }
    public decimal OtherAmount { get; init; }
    public decimal SupplyAmount { get; init; }
    public decimal TaxAmount { get; init; }
}
