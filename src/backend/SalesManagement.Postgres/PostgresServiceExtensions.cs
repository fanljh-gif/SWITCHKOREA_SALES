using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Core.Repositories;
using SalesManagement.Postgres.Repositories;
namespace SalesManagement.Postgres;
public static class PostgresServiceExtensions
{
    public static IServiceCollection AddSalesPostgres(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SalesDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUserRepository, UserRepository>(); services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>(); services.AddScoped<IPriceRepository, PriceRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>(); services.AddScoped<IRateRepository, RateRepository>();
        services.AddScoped<ITargetRepository, TargetRepository>(); services.AddScoped<IImportBatchRepository, ImportBatchRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>(); services.AddScoped<ITransactionRunner, PostgresTransactionRunner>();
        return services;
    }
}
