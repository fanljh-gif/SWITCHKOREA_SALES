namespace SalesManagement.Core.Models;
public sealed class QuoteLine : EntityBase { public Guid RevisionId { get; set; } public Guid ProductId { get; set; } public required string Description { get; set; } public decimal Quantity { get; set; } public decimal UnitPrice { get; set; } public decimal Amount { get; set; } }
