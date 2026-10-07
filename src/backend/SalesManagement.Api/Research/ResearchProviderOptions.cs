namespace SalesManagement.Api.Research;

public sealed class ResearchProviderOptions
{
    public const string SectionName = "Research";
    public string Provider { get; set; } = "Disabled";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-5.4-mini";
    public DayOfWeek WeeklyDay { get; set; } = DayOfWeek.Monday;
    public TimeOnly WeeklyTime { get; set; } = new(9, 0);
    public decimal MonthlyLimit { get; set; } = 100_000;
}
