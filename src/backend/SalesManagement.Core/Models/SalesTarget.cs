namespace SalesManagement.Core.Models;

public sealed class SalesTarget : EntityBase
{
    public YearMonth YearMonth { get; set; }
    public Guid UserId { get; set; }
    public decimal TargetKrw { get; set; }
}
