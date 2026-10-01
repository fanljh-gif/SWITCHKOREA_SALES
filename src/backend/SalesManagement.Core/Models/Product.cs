namespace SalesManagement.Core.Models;

public sealed class Product : EntityBase
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required ItemType ItemType { get; set; }
    public string Specification { get; set; } = string.Empty;
    public EntityStatus Status { get; set; } = EntityStatus.Active;
}
