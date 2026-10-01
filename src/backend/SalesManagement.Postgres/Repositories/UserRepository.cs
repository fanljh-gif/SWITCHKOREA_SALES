using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
namespace SalesManagement.Postgres.Repositories;
public sealed class UserRepository(SalesDbContext db) : EfRepository<User>(db), IUserRepository
{
    public Task<User?> FindByLoginIdAsync(string loginId, CancellationToken ct = default) => Db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.LoginId == loginId, ct);
}
