using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;

namespace SalesManagement.Postgres;

public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Price> Prices => Set<Price>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<MonthlyRate> MonthlyRates => Set<MonthlyRate>();
    public DbSet<DailyRate> DailyRates => Set<DailyRate>();
    public DbSet<SalesTarget> SalesTargets => Set<SalesTarget>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);

    public override int SaveChanges(bool acceptAllChangesOnSuccess) { PrepareVersions(); return base.SaveChanges(acceptAllChangesOnSuccess); }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) { PrepareVersions(); return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }

    private void PrepareVersions()
    {
        foreach (var entry in ChangeTracker.Entries<EntityBase>().Where(x => x.State == EntityState.Modified))
        {
            entry.Entity.Version = entry.OriginalValues.GetValue<long>(nameof(EntityBase.Version)) + 1;
            entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
