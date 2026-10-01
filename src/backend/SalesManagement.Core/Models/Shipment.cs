namespace SalesManagement.Core.Models;

public sealed class Shipment : EntityBase
{
    public string? ShipmentNumber { get; set; }
    public DateOnly ShipmentDate { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Currency Currency { get; set; }
    public decimal SupplyAmount { get; set; }
    public decimal AppliedRate { get; set; }
    public decimal KrwAmount { get; set; }
    public Guid OwnerUserId { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Confirmed;
}
