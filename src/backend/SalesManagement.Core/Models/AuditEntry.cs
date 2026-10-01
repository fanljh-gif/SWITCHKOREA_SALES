namespace SalesManagement.Core.Models;

public sealed class AuditEntry : EntityBase
{
    public Guid ActorUserId { get; set; }
    public required string Target { get; set; }
    public required string Action { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}
