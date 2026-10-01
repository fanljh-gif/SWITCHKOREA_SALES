using System.Text.Json;
using SalesManagement.Core.Auth;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Rates;

public sealed class RateService(IRateRepository rates, IAuditRepository audits)
{
    public Task<IReadOnlyList<MonthlyRate>> ListMonthlyAsync(YearMonth month, CancellationToken ct) => rates.ListMonthlyAsync(month, ct);
    public Task<IReadOnlyList<DailyRate>> ListDailyAsync(DateOnly from, DateOnly to, CancellationToken ct) => rates.ListDailyAsync(from, to, ct);

    public async Task<MonthlyRate> SaveMonthlyAsync(YearMonth month, Currency currency, decimal rate, long expectedVersion, User actor, CancellationToken ct)
    {
        AuthorizationService.EnsureManager(actor);
        if (rate <= 0m) throw new ArgumentOutOfRangeException(nameof(rate));
        var current = await rates.GetMonthlyAsync(month, currency, ct);
        var entity = new MonthlyRate
        {
            Id = current?.Id ?? Guid.NewGuid(), YearMonth = month, Currency = currency,
            Rate = currency == Currency.KRW ? 1m : rate, UpdatedBy = actor.Id
        };
        var saved = await rates.SaveMonthlyAsync(entity, expectedVersion, ct);
        await audits.AddAsync(new AuditEntry
        {
            ActorUserId = actor.Id, Target = $"monthly-rate:{month}:{currency}", Action = "update",
            BeforeJson = current is null ? null : JsonSerializer.Serialize(new { current.Rate }),
            AfterJson = JsonSerializer.Serialize(new { saved.Rate })
        }, ct);
        return saved;
    }
}
