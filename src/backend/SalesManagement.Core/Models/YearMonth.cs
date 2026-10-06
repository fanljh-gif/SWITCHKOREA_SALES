using System.Text.Json.Serialization;

namespace SalesManagement.Core.Models;

public readonly record struct YearMonth
{
    [JsonConstructor]
    public YearMonth(int year, int month)
    {
        if (year is < 1 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        Year = year;
        Month = month;
    }

    public int Year { get; }
    public int Month { get; }
    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
