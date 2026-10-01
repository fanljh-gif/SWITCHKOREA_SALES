using SalesManagement.Core.Models;
namespace SalesManagement.Core.Repositories;
public interface IRateRepository
{
    Task<IReadOnlyList<MonthlyRate>> ListMonthlyAsync(YearMonth yearMonth, CancellationToken cancellationToken = default);
    Task<MonthlyRate?> GetMonthlyAsync(YearMonth yearMonth, Currency currency, CancellationToken cancellationToken = default);
    Task<MonthlyRate> SaveMonthlyAsync(MonthlyRate rate, long expectedVersion, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyRate>> ListDailyAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<DailyRate> AddDailyAsync(DailyRate rate, CancellationToken cancellationToken = default);
}
