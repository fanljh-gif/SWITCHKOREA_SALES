using Microsoft.EntityFrameworkCore; using SalesManagement.Core.Exceptions; using SalesManagement.Core.Models; using SalesManagement.Core.Repositories;
namespace SalesManagement.Postgres.Repositories;
public sealed class InquiryRepository(SalesDbContext db) : EfRepository<CustomerInquiry>(db), IInquiryRepository
{
    private static readonly SemaphoreSlim SequenceGate = new(1, 1);
    public override async Task<IReadOnlyList<CustomerInquiry>> ListAsync(CancellationToken ct = default)
    {
        var rows = await Db.CustomerInquiries.AsNoTracking().Select(x => new CustomerInquiry
        {
            Id = x.Id, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt, Version = x.Version,
            InquiryNumber = x.InquiryNumber, Title = x.Title, CustomerId = x.CustomerId,
            CustomerName = x.CustomerName, ContactName = x.ContactName, ContactEmail = x.ContactEmail,
            OwnerUserId = x.OwnerUserId, ReceivedAt = x.ReceivedAt, ResponseDueAt = x.ResponseDueAt,
            Status = x.Status, Summary = x.Summary, ResultMemo = x.ResultMemo,
            Activities = x.Activities.Select(a => new InquiryActivity
            {
                Id = a.Id, CreatedAt = a.CreatedAt, UpdatedAt = a.UpdatedAt, Version = a.Version,
                InquiryId = a.InquiryId, Kind = a.Kind, Body = a.Body, ActorId = a.ActorId, OccurredAt = a.OccurredAt
            }).ToList(),
            Attachments = x.Attachments.Select(a => new InquiryAttachment
            {
                Id = a.Id, CreatedAt = a.CreatedAt, UpdatedAt = a.UpdatedAt, Version = a.Version,
                InquiryId = a.InquiryId, FileName = a.FileName, ContentType = a.ContentType, Sha256 = a.Sha256,
                Size = a.Size, UploadedBy = a.UploadedBy, UploadedAt = a.UploadedAt, Content = Array.Empty<byte>()
            }).ToList()
        }).ToArrayAsync(ct);
        return rows.OrderBy(x => x.CreatedAt).ToArray();
    }
    public override Task<CustomerInquiry?> GetAsync(Guid id, CancellationToken ct = default) => Db.CustomerInquiries.AsNoTracking().Include(x => x.Activities).Include(x => x.Attachments).SingleOrDefaultAsync(x => x.Id == id, ct);
    public override async Task<CustomerInquiry> UpdateAsync(CustomerInquiry entity, long expectedVersion, CancellationToken ct = default) { var current = await Db.CustomerInquiries.Include(x => x.Activities).Include(x => x.Attachments).SingleOrDefaultAsync(x => x.Id == entity.Id, ct) ?? throw new KeyNotFoundException(); if (current.Version != expectedVersion) throw new ConcurrencyException(entity.Id, expectedVersion, current.Version); Db.Entry(current).CurrentValues.SetValues(entity); Sync(current.Activities, entity.Activities); Sync(current.Attachments, entity.Attachments); await Db.SaveChangesAsync(ct); Db.ChangeTracker.Clear(); return current; }
    public async Task<int> NextSequenceAsync(int year, int month, CancellationToken ct = default) => await Next(Db, "inquiry", year, month, ct);
    internal static async Task<int> Next(SalesDbContext db, string kind, int year, int month, CancellationToken ct) {
        if (db.Database.IsNpgsql())
        {
            var connection = db.Database.GetDbConnection();
            var close = connection.State != System.Data.ConnectionState.Open;
            if (close) await connection.OpenAsync(ct);
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO quote_number_sequences (\"Year\", \"Month\", \"Kind\", \"NextValue\") VALUES (@year, @month, @kind, 1) ON CONFLICT (\"Year\", \"Month\", \"Kind\") DO UPDATE SET \"NextValue\" = quote_number_sequences.\"NextValue\" + 1 RETURNING \"NextValue\"";
                foreach (var pair in new (string Name, object Value)[] { ("year", year), ("month", month), ("kind", kind) }) { var parameter = command.CreateParameter(); parameter.ParameterName = pair.Name; parameter.Value = pair.Value; command.Parameters.Add(parameter); }
                return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
            }
            finally { if (close) await connection.CloseAsync(); }
        }
 await SequenceGate.WaitAsync(ct); try { var row = await db.QuoteNumberSequences.SingleOrDefaultAsync(x => x.Year == year && x.Month == month && x.Kind == kind, ct); if (row is null) { row = new() { Year = year, Month = month, Kind = kind, NextValue = 1 }; db.Add(row); } else row.NextValue++; await db.SaveChangesAsync(ct); return row.NextValue; } finally { SequenceGate.Release(); } }
    private void Sync<T>(ICollection<T> current, IEnumerable<T> incoming) where T : EntityBase { var ids = current.Select(x => x.Id).ToHashSet(); foreach (var item in incoming.Where(x => !ids.Contains(x.Id))) { current.Add(item); Db.Entry(item).State = EntityState.Added; } }
}
public sealed class QuoteRepository(SalesDbContext db) : EfRepository<Quote>(db), IQuoteRepository
{
    private IQueryable<Quote> Graph() => Db.Quotes.Include(x => x.Revisions).ThenInclude(x => x.Lines).Include(x => x.Revisions).ThenInclude(x => x.Approvals).Include(x => x.Revisions).ThenInclude(x => x.Dispatches);
    public override async Task<IReadOnlyList<Quote>> ListAsync(CancellationToken ct = default) => await Graph().AsNoTracking().OrderBy(x => x.CreatedAt).ToArrayAsync(ct);
    public override Task<Quote?> GetAsync(Guid id, CancellationToken ct = default) => Graph().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public override async Task<Quote> UpdateAsync(Quote entity, long expectedVersion, CancellationToken ct = default)
    {
        var current = await Graph().SingleOrDefaultAsync(x => x.Id == entity.Id, ct) ?? throw new KeyNotFoundException();
        if (current.Version != expectedVersion) throw new ConcurrencyException(entity.Id, expectedVersion, current.Version);
        foreach (var source in entity.Revisions)
        {
            var target = current.Revisions.SingleOrDefault(x => x.Id == source.Id);
            if (target is not null && !CanEditLines(current, target) && (!SameLines(target.Lines, source.Lines) || target.PaymentTerms != source.PaymentTerms || target.DeliveryTerms != source.DeliveryTerms || target.Notes != source.Notes))
                throw new InvalidDataException("승인 요청 이후 또는 과거 개정본의 품목 및 거래 조건은 변경할 수 없습니다.");
        }
        Db.Entry(current).CurrentValues.SetValues(entity);
        // Child-only changes must advance the aggregate version too.
        Db.Entry(current).State = EntityState.Modified;
        foreach (var source in entity.Revisions)
        {
            var target = current.Revisions.SingleOrDefault(x => x.Id == source.Id);
            if (target is null)
            {
                current.Revisions.Add(source); Db.Entry(source).State = EntityState.Added;
                foreach (var item in source.Lines.Cast<EntityBase>().Concat(source.Approvals).Concat(source.Dispatches)) Db.Entry(item).State = EntityState.Added;
                continue;
            }
            if (CanEditLines(current, target))
            {
                var incomingIds = source.Lines.Select(x => x.Id).ToHashSet();
                foreach (var removed in target.Lines.Where(x => !incomingIds.Contains(x.Id)).ToArray())
                {
                    target.Lines.Remove(removed);
                    Db.Entry(removed).State = EntityState.Deleted;
                }
            }
            Db.Entry(target).CurrentValues.SetValues(source);
            Merge(target.Lines, source.Lines);
            // Approval and dispatch history is append/update only, never synchronized by deletion.
            Merge(target.Approvals, source.Approvals);
            Merge(target.Dispatches, source.Dispatches);
        }
        await Db.SaveChangesAsync(ct); Db.ChangeTracker.Clear(); return current;
    }
    public Task<int> NextSequenceAsync(int year, int month, CancellationToken ct = default) => InquiryRepository.Next(Db, "quote", year, month, ct);
    private static bool CanEditLines(Quote quote, QuoteRevision revision) =>
        quote.CurrentRevisionId == revision.Id && revision.Status is QuoteStatus.Draft or QuoteStatus.Rejected;
    private static bool SameLines(IReadOnlyCollection<QuoteLine> left, IReadOnlyCollection<QuoteLine> right) =>
        left.Count == right.Count && left.All(a => right.Any(b => a.Id == b.Id && a.ProductId == b.ProductId &&
            a.Description == b.Description && a.Quantity == b.Quantity && a.UnitPrice == b.UnitPrice && a.Amount == b.Amount));
    private void Merge<T>(ICollection<T> target, IEnumerable<T> incoming) where T : EntityBase { foreach (var item in incoming) { var current = target.SingleOrDefault(x => x.Id == item.Id); if (current is null) { target.Add(item); Db.Entry(item).State = EntityState.Added; } else Db.Entry(current).CurrentValues.SetValues(item); } }
}
public sealed class IncotermRepository(SalesDbContext db) : EfRepository<Incoterm>(db), IIncotermRepository { public Task<Incoterm?> FindByCodeAsync(string code, CancellationToken ct = default) => Db.Incoterms.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code.ToUpper(), ct); }
