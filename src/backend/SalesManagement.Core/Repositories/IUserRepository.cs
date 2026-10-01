using SalesManagement.Core.Models;
namespace SalesManagement.Core.Repositories;
public interface IUserRepository : IRepository<User> { Task<User?> FindByLoginIdAsync(string loginId, CancellationToken cancellationToken = default); }
