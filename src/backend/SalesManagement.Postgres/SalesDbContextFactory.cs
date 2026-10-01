using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SalesManagement.Postgres;

public sealed class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__SalesManagement")
            ?? "Host=localhost;Database=switch_korea_sales;Username=postgres";
        return new SalesDbContext(new DbContextOptionsBuilder<SalesDbContext>().UseNpgsql(connection).Options);
    }
}
