using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesManagement.Core.Models;

namespace SalesManagement.Postgres.Configurations;

public sealed class NetworkCompanyConfiguration : IEntityTypeConfiguration<NetworkCompany>
{
    public void Configure(EntityTypeBuilder<NetworkCompany> builder)
    {
        builder.ConfigureEntity("network_companies");
        builder.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Industry).HasMaxLength(150).IsRequired();
        builder.Property(x => x.LifecycleType).HasConversion<string>();
        builder.Property(x => x.Status).HasConversion<string>();
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CurrentOwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.CustomerId).IsUnique().HasFilter("\"CustomerId\" IS NOT NULL");
        builder.HasIndex(x => x.CurrentOwnerUserId);
    }
}

public sealed class CompanyRelationshipConfiguration : IEntityTypeConfiguration<CompanyRelationship>
{
    public void Configure(EntityTypeBuilder<CompanyRelationship> builder)
    {
        builder.ConfigureEntity("company_relationships");
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>();
        builder.HasOne<NetworkCompany>().WithMany().HasForeignKey(x => x.UpstreamCompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<NetworkCompany>().WithMany().HasForeignKey(x => x.DownstreamCompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Supplies).WithOne().HasForeignKey(x => x.RelationshipId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UpstreamCompanyId, x.DownstreamCompanyId, x.ValidFrom }).IsUnique().HasFilter("\"Status\" = 'Active'");
    }
}

public sealed class RelationshipSupplyConfiguration : IEntityTypeConfiguration<RelationshipSupply>
{
    public void Configure(EntityTypeBuilder<RelationshipSupply> builder)
    {
        builder.ConfigureEntity("relationship_supplies");
        builder.Property(x => x.ApplicationName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.RelationshipId, x.ProductId, x.ApplicationName }).IsUnique();
    }
}

public sealed class CompanyContactConfiguration : IEntityTypeConfiguration<CompanyContact>
{
    public void Configure(EntityTypeBuilder<CompanyContact> builder)
    {
        builder.ConfigureEntity("company_contacts");
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Department).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Position).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BusinessRole).HasMaxLength(150).IsRequired();
        builder.Property(x => x.MobilePhone).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.EmploymentStatus).HasConversion<string>();
        builder.HasOne<NetworkCompany>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.CompanyId);
    }
}

public sealed class CompanyAssignmentConfiguration : IEntityTypeConfiguration<CompanyAssignment>
{
    public void Configure(EntityTypeBuilder<CompanyAssignment> builder)
    {
        builder.ConfigureEntity("company_assignments");
        builder.HasOne<NetworkCompany>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.CompanyId).IsUnique().HasFilter("\"IsCurrent\"");
        builder.HasIndex(x => new { x.CompanyId, x.ValidFrom }).IsUnique();
    }
}
