using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Dashboard;

public sealed record DashboardQuery(DateOnly From, DateOnly To, Guid? OwnerId = null, Guid? CustomerId = null, Guid? ProductId = null);
public sealed record CurrencyTotal(Currency Currency, decimal OriginalAmount, decimal KrwAmount, decimal SharePercent);
public sealed record GroupTotal(Guid Id, decimal KrwAmount);
public sealed record DashboardSummary(decimal KrwTotal, decimal TargetKrw, decimal? AchievementRate,
    IReadOnlyList<CurrencyTotal> ByCurrency, IReadOnlyList<GroupTotal> ByOwner,
    IReadOnlyList<GroupTotal> ByCustomer, IReadOnlyList<GroupTotal> ByProduct);

public sealed class DashboardService(IShipmentRepository shipments, ITargetRepository targets)
{
    public async Task<DashboardSummary> QueryAsync(DashboardQuery query, CancellationToken ct)
    {
        if (query.To < query.From) throw new ArgumentException("종료일은 시작일보다 빠를 수 없습니다.");
        var data = (await shipments.ListByDateRangeAsync(query.From, query.To, ct))
            .Where(x => x.Status == ShipmentStatus.Confirmed)
            .Where(x => query.OwnerId is null || x.OwnerUserId == query.OwnerId)
            .Where(x => query.CustomerId is null || x.CustomerId == query.CustomerId)
            .Where(x => query.ProductId is null || x.ProductId == query.ProductId)
            .ToArray();
        var total = data.Sum(x => x.KrwAmount);
        var target = await SumTargets(query, ct);
        return new DashboardSummary(
            total, target, target == 0m ? null : decimal.Round(total / target * 100m, 1),
            data.GroupBy(x => x.Currency).Select(g => new CurrencyTotal(g.Key, g.Sum(x => x.SupplyAmount), g.Sum(x => x.KrwAmount), total == 0m ? 0m : decimal.Round(g.Sum(x => x.KrwAmount) / total * 100m, 1))).OrderBy(x => x.Currency).ToArray(),
            Group(data, x => x.OwnerUserId), Group(data, x => x.CustomerId), Group(data, x => x.ProductId));
    }

    private async Task<decimal> SumTargets(DashboardQuery query, CancellationToken ct)
    {
        var cursor = new YearMonth(query.From.Year, query.From.Month);
        var end = new YearMonth(query.To.Year, query.To.Month);
        decimal total = 0m;
        while (cursor.Year < end.Year || (cursor.Year == end.Year && cursor.Month <= end.Month))
        {
            var monthTargets = await targets.ListByMonthAsync(cursor, ct);
            total += monthTargets.Where(x => query.OwnerId is null || x.UserId == query.OwnerId).Sum(x => x.TargetKrw);
            cursor = cursor.Month == 12 ? new YearMonth(cursor.Year + 1, 1) : new YearMonth(cursor.Year, cursor.Month + 1);
        }
        return total;
    }

    private static GroupTotal[] Group(IEnumerable<Shipment> source, Func<Shipment, Guid> key) =>
        source.GroupBy(key).Select(g => new GroupTotal(g.Key, g.Sum(x => x.KrwAmount))).OrderByDescending(x => x.KrwAmount).ToArray();
}
