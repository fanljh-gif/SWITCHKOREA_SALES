namespace SalesManagement.Core.Models;

public sealed class CompanyAssignment : EntityBase
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsCurrent { get; set; }
}
