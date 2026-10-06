namespace SalesManagement.Core.Models;

public sealed class CompanyRelationship : EntityBase
{
    public Guid UpstreamCompanyId { get; set; }
    public Guid DownstreamCompanyId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public string? Notes { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public List<RelationshipSupply> Supplies { get; set; } = [];
}
