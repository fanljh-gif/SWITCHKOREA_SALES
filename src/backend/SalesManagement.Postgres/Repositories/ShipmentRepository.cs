using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
namespace SalesManagement.Postgres.Repositories;
public sealed class ShipmentRepository(SalesDbContext db) : EfRepository<Shipment>(db), IShipmentRepository
{
    public async Task<IReadOnlyList<Shipment>> ListByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default) => await Db.Shipments.AsNoTracking().Where(x => x.ShipmentDate >= from && x.ShipmentDate <= to).ToArrayAsync(ct);
}
