namespace SalesManagement.Core.Models;

public sealed class Price : EntityBase
{
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public Currency Currency { get; set; }
    public decimal UnitPrice { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
