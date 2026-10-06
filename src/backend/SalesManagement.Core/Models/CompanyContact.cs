namespace SalesManagement.Core.Models;

public sealed class CompanyContact : EntityBase
{
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public required string Department { get; set; }
    public required string Position { get; set; }
    public required string BusinessRole { get; set; }
    public required string MobilePhone { get; set; }
    public required string Email { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;
}
