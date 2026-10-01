using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Postgres.Repositories;

public abstract class EfRepository<T>(SalesDbContext db) : IRepository<T> where T : EntityBase
{
    protected SalesDbContext Db { get; } = db;
    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) => await Db.Set<T>().AsNoTracking().OrderBy(x => x.CreatedAt).ToArrayAsync(ct);
    public Task<T?> GetAsync(Guid id, CancellationToken ct = default) => Db.Set<T>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<T> AddAsync(T entity, CancellationToken ct = default) { Db.Add(entity); await Db.SaveChangesAsync(ct); return entity; }
    public async Task<T> UpdateAsync(T entity, long expectedVersion, CancellationToken ct = default)
    {
        Db.Attach(entity); Db.Entry(entity).State = EntityState.Modified; Db.Entry(entity).Property(x => x.Version).OriginalValue = expectedVersion;
        try { await Db.SaveChangesAsync(ct); return entity; }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyException(entity.Id, expectedVersion, -1); }
    }
}
