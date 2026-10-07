using SalesManagement.Core.Models;

namespace SalesManagement.Core.Research;

public sealed record UsageSnapshot(decimal UsedKrw, decimal LimitKrw)
{
    public decimal Percent => LimitKrw <= 0 ? 0 : Math.Min(100, decimal.Round(UsedKrw / LimitKrw * 100, 0));
}
public sealed record BudgetDecision(bool Allowed, string? WarningCode);

public sealed class UsageBudgetService
{
    public BudgetDecision CanStart(ResearchRunKind kind, UsageSnapshot usage, User actor)
    {
        var percent = usage.Percent;
        if (kind == ResearchRunKind.Manual && actor.Role != Role.Manager) return new(false, "managerOnly");
        if (kind == ResearchRunKind.Scheduled && percent >= 100) return new(false, "100");
        if (percent >= 100) return new(true, "100");
        if (percent >= 90) return new(true, "90");
        if (percent >= 70) return new(true, "70");
        return new(true, null);
    }
}
