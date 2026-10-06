namespace SalesManagement.Core.Network;

public static class SeoulBusinessDate
{
    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9));
    public static DateOnly FromUtc(DateTimeOffset instant) => DateOnly.FromDateTime(instant.UtcDateTime.AddHours(9));
}
