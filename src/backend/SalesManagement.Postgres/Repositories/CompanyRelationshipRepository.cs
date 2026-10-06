using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Postgres.Repositories;

public sealed class CompanyRelationshipRepository(SalesDbContext db) : EfRepository<CompanyRelationship>(db), ICompanyRelationshipRepository
{
    public override async Task<CompanyRelationship> AddAsync(CompanyRelationship entity, CancellationToken ct = default)
    {
        var saved = await base.AddAsync(entity, ct);
        Db.ChangeTracker.Clear();
        return saved;
    }

    public override async Task<CompanyRelationship> UpdateAsync(CompanyRelationship entity, long expectedVersion, CancellationToken ct = default)
    {
        var tracked = await Db.CompanyRelationships.Include(x => x.Supplies).SingleOrDefaultAsync(x => x.Id == entity.Id, ct)
            ?? throw new KeyNotFoundException("관계를 찾을 수 없습니다.");
        Db.Entry(tracked).State = EntityState.Modified;
        Db.Entry(tracked).Property(x => x.Version).OriginalValue = expectedVersion;
        tracked.UpstreamCompanyId = entity.UpstreamCompanyId;
        tracked.DownstreamCompanyId = entity.DownstreamCompanyId;
        tracked.ValidFrom = entity.ValidFrom;
        tracked.ValidTo = entity.ValidTo;
        tracked.Notes = entity.Notes;
        tracked.Status = entity.Status;
        Db.RelationshipSupplies.RemoveRange(tracked.Supplies);
        tracked.Supplies = entity.Supplies.Select(x => new RelationshipSupply
        {
            Id = x.Id,
            RelationshipId = tracked.Id,
            ProductId = x.ProductId,
            ApplicationName = x.ApplicationName,
            Notes = x.Notes,
        }).ToList();
        foreach (var supply in tracked.Supplies) Db.Entry(supply).State = EntityState.Added;
        try
        {
            await Db.SaveChangesAsync(ct);
            entity.Version = tracked.Version;
            entity.UpdatedAt = tracked.UpdatedAt;
            Db.ChangeTracker.Clear();
            return entity;
        }
        catch (DbUpdateConcurrencyException)
        {
            Db.ChangeTracker.Clear();
            throw new ConcurrencyException(entity.Id, expectedVersion, -1);
        }
    }

    public override async Task<IReadOnlyList<CompanyRelationship>> ListAsync(CancellationToken ct = default) =>
        (await Db.CompanyRelationships.AsNoTracking().Include(x => x.Supplies).ToArrayAsync(ct)).OrderBy(x => x.CreatedAt).ToArray();

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
