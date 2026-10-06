namespace SalesManagement.Core.Models;

public sealed class RelationshipSupply : EntityBase
{
    public Guid RelationshipId { get; set; }
    public Guid ProductId { get; set; }
    public required string ApplicationName { get; set; }
    public string? Notes { get; set; }
}
