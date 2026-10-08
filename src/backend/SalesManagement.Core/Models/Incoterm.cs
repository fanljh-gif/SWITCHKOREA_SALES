namespace SalesManagement.Core.Models;
public sealed class Incoterm : EntityBase { public required string Code { get; set; } public required string DisplayName { get; set; } public EntityStatus Status { get; set; } = EntityStatus.Active; public int SortOrder { get; set; } }
