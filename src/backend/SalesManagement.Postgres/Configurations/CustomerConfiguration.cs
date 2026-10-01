using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesManagement.Core.Models;

namespace SalesManagement.Postgres.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ConfigureEntity("customers"); builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(50); builder.Property(x => x.CompanyName).HasMaxLength(200);
        builder.Property(x => x.Country).HasMaxLength(100); builder.Property(x => x.DefaultCurrency).HasConversion<string>(); builder.Property(x => x.Status).HasConversion<string>();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
