using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
namespace SalesManagement.Postgres.Repositories;
public sealed class PriceRepository(SalesDbContext db) : EfRepository<Price>(db), IPriceRepository
{
    public Task<Price?> FindEffectiveAsync(Guid customerId, Guid productId, DateOnly date, CancellationToken ct = default) => Db.Prices.AsNoTracking().Where(x => x.CustomerId == customerId && x.ProductId == productId && x.ValidFrom <= date && (x.ValidTo == null || x.ValidTo >= date)).OrderByDescending(x => x.ValidFrom).FirstOrDefaultAsync(ct);
}
