using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
namespace SalesManagement.Postgres.Repositories;
public sealed class ImportBatchRepository(SalesDbContext db) : EfRepository<ImportBatch>(db), IImportBatchRepository;
public sealed class AuditRepository(SalesDbContext db) : EfRepository<AuditEntry>(db), IAuditRepository
{
    public async Task<IReadOnlyList<AuditEntry>> ListByTargetAsync(string target, CancellationToken ct = default) => await Db.AuditEntries.AsNoTracking().Where(x => x.Target == target).OrderByDescending(x => x.CreatedAt).ToArrayAsync(ct);
}
