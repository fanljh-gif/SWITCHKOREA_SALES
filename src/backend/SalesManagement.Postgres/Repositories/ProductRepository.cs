using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
namespace SalesManagement.Postgres.Repositories;
public sealed class ProductRepository(SalesDbContext db) : EfRepository<Product>(db), IProductRepository;
