namespace SalesManagement.Core.Network;

internal static class SeoulBusinessDate
{
    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9));
}
