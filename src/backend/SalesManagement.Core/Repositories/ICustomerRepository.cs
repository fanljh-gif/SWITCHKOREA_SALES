using SalesManagement.Core.Models;
namespace SalesManagement.Core.Repositories;
public interface ICustomerRepository : IRepository<Customer> { Task<IReadOnlyList<Customer>> ListByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default); }
