using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Rates;

public interface IDailyRateProvider
{
    Task<decimal> FetchAsync(DateOnly date, Currency currency, CancellationToken cancellationToken);
}

public sealed class DailyRateCollector(IRateRepository rates, IDailyRateProvider provider, TimeProvider clock)
{
    public async Task<DailyRate> CollectAsync(DateOnly date, Currency currency, CancellationToken ct)
    {
        if (currency == Currency.KRW) return await Save(date, currency, 1m, RateFetchStatus.Success, ct);
        try
        {
            var value = await provider.FetchAsync(date, currency, ct);
            if (value <= 0m) throw new InvalidOperationException("환율은 0보다 커야 합니다.");
            return await Save(date, currency, value, RateFetchStatus.Success, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch
        {
            var history = await rates.ListDailyAsync(DateOnly.MinValue, date.AddDays(-1), ct);
            var last = history.Where(x => x.Currency == currency && x.FetchStatus == RateFetchStatus.Success)
                .OrderByDescending(x => x.Date).ThenByDescending(x => x.FetchedAt).FirstOrDefault();
            return await Save(date, currency, last?.Rate ?? 0m, RateFetchStatus.Failed, ct);
        }
    }

    private Task<DailyRate> Save(DateOnly date, Currency currency, decimal value, RateFetchStatus status, CancellationToken ct) =>
        rates.AddDailyAsync(new DailyRate { Date = date, Currency = currency, Rate = value, FetchStatus = status, FetchedAt = clock.GetUtcNow() }, ct);
}
