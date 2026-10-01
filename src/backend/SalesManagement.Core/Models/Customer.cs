namespace SalesManagement.Core.Models;

public sealed class Customer : EntityBase
{
    public required string Code { get; set; }
    public required string CompanyName { get; set; }
    public required string Country { get; set; }
    public Currency DefaultCurrency { get; set; }
    public Guid OwnerUserId { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
}
