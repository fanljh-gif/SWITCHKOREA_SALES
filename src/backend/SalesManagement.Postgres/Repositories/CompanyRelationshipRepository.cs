using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Postgres.Repositories;

public sealed class CompanyRelationshipRepository(SalesDbContext db) : EfRepository<CompanyRelationship>(db), ICompanyRelationshipRepository
{
    public override async Task<IReadOnlyList<CompanyRelationship>> ListAsync(CancellationToken ct = default) =>
        await Db.CompanyRelationships.AsNoTracking().Include(x => x.Supplies).OrderBy(x => x.CreatedAt).ToArrayAsync(ct);

    public override Task<CompanyRelationship?> GetAsync(Guid id, CancellationToken ct = default) =>
        Db.CompanyRelationships.AsNoTracking().Include(x => x.Supplies).SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<CompanyRelationship>> ListByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        await Db.CompanyRelationships.AsNoTracking().Include(x => x.Supplies)
            .Where(x => x.UpstreamCompanyId == companyId || x.DownstreamCompanyId == companyId).OrderBy(x => x.ValidFrom).ToArrayAsync(ct);

    public async Task<IReadOnlyList<CompanyRelationship>> ListByRootAsync(Guid rootCompanyId, CancellationToken ct = default)
    {
        var result = new List<CompanyRelationship>();
        var pending = new Stack<Guid>(); var expanded = new HashSet<Guid>(); pending.Push(rootCompanyId);
        while (pending.TryPop(out var companyId) && expanded.Add(companyId))
        {
            var rows = await Db.CompanyRelationships.AsNoTracking().Include(x => x.Supplies)
                .Where(x => x.UpstreamCompanyId == companyId && x.Status == EntityStatus.Active).ToArrayAsync(ct);
            result.AddRange(rows);
            foreach (var row in rows) pending.Push(row.DownstreamCompanyId);
        }
        return result;
    }
}
