using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
namespace SalesManagement.Postgres.Repositories;
public sealed class CustomerRepository(SalesDbContext db) : EfRepository<Customer>(db), ICustomerRepository
{
    public async Task<IReadOnlyList<Customer>> ListByOwnerAsync(Guid ownerUserId, CancellationToken ct = default) => await Db.Customers.AsNoTracking().Where(x => x.OwnerUserId == ownerUserId).ToArrayAsync(ct);
}
