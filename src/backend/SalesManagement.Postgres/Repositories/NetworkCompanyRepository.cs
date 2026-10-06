using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Postgres.Repositories;

public sealed class NetworkCompanyRepository(SalesDbContext db) : EfRepository<NetworkCompany>(db), INetworkCompanyRepository
{
    public async Task<IReadOnlyList<NetworkCompany>> ListByOwnerAsync(Guid ownerUserId, CancellationToken ct = default) =>
        await Db.NetworkCompanies.AsNoTracking().Where(x => x.CurrentOwnerUserId == ownerUserId).OrderBy(x => x.CompanyName).ToArrayAsync(ct);
}
