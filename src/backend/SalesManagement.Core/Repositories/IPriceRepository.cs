using SalesManagement.Core.Models;
namespace SalesManagement.Core.Repositories;
public interface IPriceRepository : IRepository<Price> { Task<Price?> FindEffectiveAsync(Guid customerId, Guid productId, DateOnly date, CancellationToken cancellationToken = default); }
