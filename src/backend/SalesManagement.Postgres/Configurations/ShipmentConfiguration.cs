using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesManagement.Core.Models;

namespace SalesManagement.Postgres.Configurations;

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ConfigureEntity("shipments"); builder.Property(x => x.ShipmentNumber).HasMaxLength(100);
        builder.Property(x => x.Currency).HasConversion<string>(); builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.Quantity).HasPrecision(20, 4); builder.Property(x => x.UnitPrice).HasPrecision(20, 4);
        builder.Property(x => x.SupplyAmount).HasPrecision(20, 4); builder.Property(x => x.AppliedRate).HasPrecision(20, 8); builder.Property(x => x.KrwAmount).HasPrecision(20, 4);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CustomerId, x.ShipmentNumber }).IsUnique().HasFilter("\"ShipmentNumber\" IS NOT NULL");
    }
}
