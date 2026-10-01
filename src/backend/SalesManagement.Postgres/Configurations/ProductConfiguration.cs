using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesManagement.Core.Models;

namespace SalesManagement.Postgres.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ConfigureEntity("products"); builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(50); builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.ItemType).HasConversion<string>(); builder.Property(x => x.Status).HasConversion<string>();
        builder.ToTable("products", table => table.HasCheckConstraint("CK_products_item_type", "\"ItemType\" IN ('Product','Goods')"));
    }
}
