using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
namespace SalesManagement.Postgres.Repositories;
public sealed class RateRepository(SalesDbContext db) : IRateRepository
{
    public async Task<IReadOnlyList<MonthlyRate>> ListMonthlyAsync(YearMonth month, CancellationToken ct = default) => await db.MonthlyRates.AsNoTracking().Where(x => x.YearMonth == month).ToArrayAsync(ct);
    public Task<MonthlyRate?> GetMonthlyAsync(YearMonth month, Currency currency, CancellationToken ct = default) => db.MonthlyRates.AsNoTracking().SingleOrDefaultAsync(x => x.YearMonth == month && x.Currency == currency, ct);
    public async Task<MonthlyRate> SaveMonthlyAsync(MonthlyRate rate, long expectedVersion, CancellationToken ct = default) { if (await db.MonthlyRates.AnyAsync(x => x.Id == rate.Id, ct)) { db.Attach(rate); db.Entry(rate).State = EntityState.Modified; db.Entry(rate).Property(x => x.Version).OriginalValue = expectedVersion; } else db.Add(rate); await db.SaveChangesAsync(ct); return rate; }
    public async Task<IReadOnlyList<DailyRate>> ListDailyAsync(DateOnly from, DateOnly to, CancellationToken ct = default) => await db.DailyRates.AsNoTracking().Where(x => x.Date >= from && x.Date <= to).ToArrayAsync(ct);
    public async Task<DailyRate> AddDailyAsync(DailyRate rate, CancellationToken ct = default) { db.Add(rate); await db.SaveChangesAsync(ct); return rate; }
}
public sealed class TargetRepository(SalesDbContext db) : EfRepository<SalesTarget>(db), ITargetRepository
{
    public async Task<IReadOnlyList<SalesTarget>> ListByMonthAsync(YearMonth month, CancellationToken ct = default) => await Db.SalesTargets.AsNoTracking().Where(x => x.YearMonth == month).ToArrayAsync(ct);
}
