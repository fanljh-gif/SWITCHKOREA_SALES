using SalesManagement.Core.Models;
namespace SalesManagement.Core.Repositories;
public interface ICompanyRelationshipRepository : IRepository<CompanyRelationship>
{
    Task<IReadOnlyList<CompanyRelationship>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyRelationship>> ListByRootAsync(Guid rootCompanyId, CancellationToken cancellationToken = default);
}
