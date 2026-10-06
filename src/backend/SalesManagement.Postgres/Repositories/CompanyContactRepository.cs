using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Postgres.Repositories;

public sealed class CompanyContactRepository(SalesDbContext db) : EfRepository<CompanyContact>(db), ICompanyContactRepository
{
    public async Task<IReadOnlyList<CompanyContact>> ListByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        await Db.CompanyContacts.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.ValidFrom).ToArrayAsync(ct);
}
