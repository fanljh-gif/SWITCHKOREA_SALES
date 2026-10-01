using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesManagement.Core.Models;

namespace SalesManagement.Postgres.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ConfigureEntity("users"); builder.HasIndex(x => x.LoginId).IsUnique();
        builder.Property(x => x.LoginId).HasMaxLength(100); builder.Property(x => x.PasswordHash).HasMaxLength(500);
        builder.Property(x => x.Name).HasMaxLength(100); builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.Role).HasConversion<string>(); builder.Property(x => x.Status).HasConversion<string>();
    }
}
