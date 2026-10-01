namespace SalesManagement.Core.Models;

public sealed class User : EntityBase
{
    public required string LoginId { get; set; }
    public required string PasswordHash { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public Role Role { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public bool PasswordChangeRequired { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
}
