using SalesManagement.Core.Models;
namespace SalesManagement.Core.Repositories;
public interface INetworkCompanyRepository : IRepository<NetworkCompany>
{
    Task<IReadOnlyList<NetworkCompany>> ListByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
}
