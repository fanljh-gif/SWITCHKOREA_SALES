namespace SalesManagement.Core.Models;

public sealed class NetworkCompany : EntityBase
{
    public required string CompanyName { get; set; }
    public required string Country { get; set; }
    public required string Industry { get; set; }
    public LifecycleType LifecycleType { get; set; }
    public bool IsOem { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid CurrentOwnerUserId { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
}
