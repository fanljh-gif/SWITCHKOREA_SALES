using SalesManagement.Core.Models;
using SalesManagement.Core.Rates;

namespace SalesManagement.Api.Workers;

public sealed class DailyRateWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<DailyRateWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = clock.GetLocalNow();
            var next = new DateTimeOffset(now.Year, now.Month, now.Day, 9, 0, 0, now.Offset);
            if (next <= now) next = next.AddDays(1);
            await Task.Delay(next - now, clock, stoppingToken);
            using var scope = scopes.CreateScope();
            var collector = scope.ServiceProvider.GetRequiredService<DailyRateCollector>();
            foreach (var currency in new[] { Currency.USD, Currency.CNY })
            {
                var result = await collector.CollectAsync(DateOnly.FromDateTime(next.DateTime), currency, stoppingToken);
                if (result.FetchStatus == RateFetchStatus.Failed) logger.LogWarning("Daily {Currency} rate collection failed; fallback value {Rate} was saved.", currency, result.Rate);
            }
        }
    }
}
