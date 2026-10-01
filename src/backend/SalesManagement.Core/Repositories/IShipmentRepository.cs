using SalesManagement.Core.Models;
namespace SalesManagement.Core.Repositories;
public interface IShipmentRepository : IRepository<Shipment> { Task<IReadOnlyList<Shipment>> ListByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default); }
