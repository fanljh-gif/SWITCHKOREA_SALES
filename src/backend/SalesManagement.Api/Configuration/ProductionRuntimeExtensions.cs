using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SalesManagement.Core.Repositories;
using SalesManagement.InMemory.Repositories;
using SalesManagement.Postgres;

namespace SalesManagement.Api.Configuration;

public static class ProductionRuntimeExtensions
{
    public static IServiceCollection AddSalesProductionRuntime(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var runtime = ProductionRuntimeOptions.From(configuration);
        if (environment.IsProduction())
        {
            if (string.IsNullOrWhiteSpace(runtime.ConnectionString))
                throw new InvalidOperationException("ConnectionStrings:SalesManagement is required in Production.");
            var dataProtection = services.AddDataProtection()
                .SetApplicationName("SwitchKorea.SalesManagement");
            if (runtime.HostingMode == ProductionHostingMode.Render)
            {
                if (string.IsNullOrWhiteSpace(runtime.ExternalHostname))
                    throw new InvalidOperationException($"{ProductionRuntimeOptions.RenderExternalHostnameVariable} is required in Render mode.");
                dataProtection.PersistKeysToDbContext<SalesDbContext>();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(runtime.DataProtectionPath))
                    throw new InvalidOperationException($"{ProductionRuntimeOptions.DataProtectionPathVariable} is required in Production.");
                if (!OperatingSystem.IsWindows())
                    throw new PlatformNotSupportedException("Production data-protection keys require Windows DPAPI.");

                var keyDirectory = Directory.CreateDirectory(Path.GetFullPath(runtime.DataProtectionPath));
                dataProtection.PersistKeysToFileSystem(keyDirectory)
                    .ProtectKeysWithDpapi(protectToLocalMachine: true);
            }
        }

        if (!string.IsNullOrWhiteSpace(runtime.ConnectionString))
            services.AddSalesPostgres(runtime.ConnectionString);
        else
            AddInMemoryRepositories(services);

        services.AddHealthChecks()
            .AddCheck<DatabaseReadinessHealthCheck>("sales-database", tags: ["ready"]);
        return services;
    }

    private static void AddInMemoryRepositories(IServiceCollection services)
    {
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        services.AddSingleton<IPriceRepository, InMemoryPriceRepository>();
        services.AddSingleton<IShipmentRepository, InMemoryShipmentRepository>();
        services.AddSingleton<IRateRepository, InMemoryRateRepository>();
        services.AddSingleton<ITargetRepository, InMemoryTargetRepository>();
        services.AddSingleton<IAuditRepository, InMemoryAuditRepository>();
        services.AddSingleton<IImportBatchRepository, InMemoryImportBatchRepository>();
        services.AddSingleton<INetworkCompanyRepository, InMemoryNetworkCompanyRepository>();
        services.AddSingleton<ICompanyRelationshipRepository, InMemoryCompanyRelationshipRepository>();
        services.AddSingleton<ICompanyContactRepository, InMemoryCompanyContactRepository>();
        services.AddSingleton<ICompanyAssignmentRepository, InMemoryCompanyAssignmentRepository>();
        services.AddSingleton<ITransactionRunner>(provider => new InMemoryTransactionRunner(
            (IInMemoryTransactionalStore)provider.GetRequiredService<INetworkCompanyRepository>(),
            (IInMemoryTransactionalStore)provider.GetRequiredService<ICompanyAssignmentRepository>(),
            (IInMemoryTransactionalStore)provider.GetRequiredService<ICustomerRepository>(),
            (IInMemoryTransactionalStore)provider.GetRequiredService<IAuditRepository>()));
    }
}

public sealed class DatabaseReadinessHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetService<SalesDbContext>();
        if (db is null)
            return HealthCheckResult.Healthy("Development in-memory storage is ready.");

        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Database is reachable.")
                : HealthCheckResult.Unhealthy("Database is unreachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database is unreachable.", exception);
        }
    }
}
