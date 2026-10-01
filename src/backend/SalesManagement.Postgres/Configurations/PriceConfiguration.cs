using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesManagement.Core.Models;

namespace SalesManagement.Postgres.Configurations;

public sealed class PriceConfiguration : IEntityTypeConfiguration<Price>
{
    public void Configure(EntityTypeBuilder<Price> builder)
    {
        builder.ConfigureEntity("prices"); builder.Property(x => x.Currency).HasConversion<string>();
        builder.Property(x => x.UnitPrice).HasPrecision(20, 4);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CustomerId, x.ProductId, x.ValidFrom });
    }
}
