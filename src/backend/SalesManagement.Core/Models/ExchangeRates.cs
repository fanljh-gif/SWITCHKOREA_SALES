namespace SalesManagement.Core.Models;

public sealed class MonthlyRate : EntityBase
{
    public YearMonth YearMonth { get; set; }
    public Currency Currency { get; set; }
    public decimal Rate { get; set; }
    public Guid UpdatedBy { get; set; }
}

public sealed class DailyRate : EntityBase
{
    public DateOnly Date { get; set; }
    public Currency Currency { get; set; }
    public decimal Rate { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
    public RateFetchStatus FetchStatus { get; set; }
}
