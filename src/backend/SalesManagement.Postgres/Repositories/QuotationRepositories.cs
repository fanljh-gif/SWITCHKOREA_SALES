using Microsoft.EntityFrameworkCore; using SalesManagement.Core.Exceptions; using SalesManagement.Core.Models; using SalesManagement.Core.Repositories;
namespace SalesManagement.Postgres.Repositories;
public sealed class InquiryRepository(SalesDbContext db) : EfRepository<CustomerInquiry>(db), IInquiryRepository
{
    private static readonly SemaphoreSlim SequenceGate = new(1, 1);
    public override async Task<IReadOnlyList<CustomerInquiry>> ListAsync(CancellationToken ct = default) => await Db.CustomerInquiries.AsNoTracking().Include(x => x.Activities).Include(x => x.Attachments).OrderBy(x => x.CreatedAt).ToArrayAsync(ct);
    public override Task<CustomerInquiry?> GetAsync(Guid id, CancellationToken ct = default) => Db.CustomerInquiries.AsNoTracking().Include(x => x.Activities).Include(x => x.Attachments).SingleOrDefaultAsync(x => x.Id == id, ct);
    public override async Task<CustomerInquiry> UpdateAsync(CustomerInquiry entity, long expectedVersion, CancellationToken ct = default) { var current = await Db.CustomerInquiries.Include(x => x.Activities).Include(x => x.Attachments).SingleOrDefaultAsync(x => x.Id == entity.Id, ct) ?? throw new KeyNotFoundException(); if (current.Version != expectedVersion) throw new ConcurrencyException(entity.Id, expectedVersion, current.Version); Db.Entry(current).CurrentValues.SetValues(entity); Sync(current.Activities, entity.Activities); Sync(current.Attachments, entity.Attachments); await Db.SaveChangesAsync(ct); Db.ChangeTracker.Clear(); return entity; }
    public async Task<int> NextSequenceAsync(int year, int month, CancellationToken ct = default) => await Next(Db, "inquiry", year, month, ct);
    internal static async Task<int> Next(SalesDbContext db, string kind, int year, int month, CancellationToken ct) { await SequenceGate.WaitAsync(ct); try { var row = await db.QuoteNumberSequences.SingleOrDefaultAsync(x => x.Year == year && x.Month == month && x.Kind == kind, ct); if (row is null) { row = new() { Year = year, Month = month, Kind = kind, NextValue = 1 }; db.Add(row); } else row.NextValue++; await db.SaveChangesAsync(ct); return row.NextValue; } finally { SequenceGate.Release(); } }
    private void Sync<T>(ICollection<T> current, IEnumerable<T> incoming) where T : EntityBase { var ids = current.Select(x => x.Id).ToHashSet(); foreach (var item in incoming.Where(x => !ids.Contains(x.Id))) { current.Add(item); Db.Entry(item).State = EntityState.Added; } }
}
public sealed class QuoteRepository(SalesDbContext db) : EfRepository<Quote>(db), IQuoteRepository
{
    private IQueryable<Quote> Graph() => Db.Quotes.Include(x => x.Revisions).ThenInclude(x => x.Lines).Include(x => x.Revisions).ThenInclude(x => x.Approvals).Include(x => x.Revisions).ThenInclude(x => x.Dispatches);
    public override async Task<IReadOnlyList<Quote>> ListAsync(CancellationToken ct = default) => await Graph().AsNoTracking().OrderBy(x => x.CreatedAt).ToArrayAsync(ct);
    public override Task<Quote?> GetAsync(Guid id, CancellationToken ct = default) => Graph().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public override async Task<Quote> UpdateAsync(Quote entity, long expectedVersion, CancellationToken ct = default)
    {
        var current = await Graph().SingleOrDefaultAsync(x => x.Id == entity.Id, ct) ?? throw new KeyNotFoundException(); if (current.Version != expectedVersion) throw new ConcurrencyException(entity.Id, expectedVersion, current.Version); Db.Entry(current).CurrentValues.SetValues(entity);
        foreach (var source in entity.Revisions) { var target = current.Revisions.SingleOrDefault(x => x.Id == source.Id); if (target is null) { current.Revisions.Add(source); Db.Entry(source).State = EntityState.Added; foreach (var item in source.Lines.Cast<EntityBase>().Concat(source.Approvals).Concat(source.Dispatches)) Db.Entry(item).State = EntityState.Added; continue; } Db.Entry(target).CurrentValues.SetValues(source); Merge(target.Lines, source.Lines); Merge(target.Approvals, source.Approvals); Merge(target.Dispatches, source.Dispatches); }
        await Db.SaveChangesAsync(ct); Db.ChangeTracker.Clear(); return entity;
    }
    public Task<int> NextSequenceAsync(int year, int month, CancellationToken ct = default) => InquiryRepository.Next(Db, "quote", year, month, ct);
    private void Merge<T>(ICollection<T> target, IEnumerable<T> incoming) where T : EntityBase { foreach (var item in incoming) { var current = target.SingleOrDefault(x => x.Id == item.Id); if (current is null) { target.Add(item); Db.Entry(item).State = EntityState.Added; } else Db.Entry(current).CurrentValues.SetValues(item); } }
}
public sealed class IncotermRepository(SalesDbContext db) : EfRepository<Incoterm>(db), IIncotermRepository { public Task<Incoterm?> FindByCodeAsync(string code, CancellationToken ct = default) => Db.Incoterms.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code.ToUpper(), ct); }
