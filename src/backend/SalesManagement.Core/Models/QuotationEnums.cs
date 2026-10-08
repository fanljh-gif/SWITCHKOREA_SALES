namespace SalesManagement.Core.Models;

public enum InquiryStatus { Received, Assigned, Responding, Quoting, Answered, Closed }
public enum InquiryActivityKind { Response, Memo, Status, Assignment }
public enum QuoteStatus { Draft, ApprovalRequested, Approved, Rejected, Sent, Won, Lost, OnHold }
public enum QuoteTaxType { Exclusive, Inclusive, Exempt }
public enum QuoteTradeType { Domestic, Export }
public enum QuoteLanguage { Ko, En }
public enum QuoteApprovalDecision { Pending, Approved, Rejected }
