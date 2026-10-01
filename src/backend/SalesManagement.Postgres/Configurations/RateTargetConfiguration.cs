using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesManagement.Core.Models;

namespace SalesManagement.Postgres.Configurations;

public sealed class MonthlyRateConfiguration : IEntityTypeConfiguration<MonthlyRate>
{
    public void Configure(EntityTypeBuilder<MonthlyRate> builder)
    {
        builder.ConfigureEntity("monthly_rates"); builder.Property(x => x.YearMonth).HasConversion(v => ConfigurationSupport.MonthToString(v), v => ConfigurationSupport.StringToMonth(v)).HasMaxLength(7);
        builder.Property(x => x.Currency).HasConversion<string>(); builder.Property(x => x.Rate).HasPrecision(20, 8); builder.HasIndex(x => new { x.YearMonth, x.Currency }).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class DailyRateConfiguration : IEntityTypeConfiguration<DailyRate>
{
    public void Configure(EntityTypeBuilder<DailyRate> builder)
    {
        builder.ConfigureEntity("daily_rates"); builder.Property(x => x.Currency).HasConversion<string>(); builder.Property(x => x.Rate).HasPrecision(20, 8); builder.Property(x => x.FetchedAt).HasColumnType("timestamptz"); builder.Property(x => x.FetchStatus).HasConversion<string>(); builder.HasIndex(x => new { x.Date, x.Currency }).IsUnique();
    }
}
public sealed class SalesTargetConfiguration : IEntityTypeConfiguration<SalesTarget>
{
    public void Configure(EntityTypeBuilder<SalesTarget> builder)
    {
        builder.ConfigureEntity("sales_targets"); builder.Property(x => x.YearMonth).HasConversion(v => ConfigurationSupport.MonthToString(v), v => ConfigurationSupport.StringToMonth(v)).HasMaxLength(7); builder.Property(x => x.TargetKrw).HasPrecision(20, 4); builder.HasIndex(x => new { x.UserId, x.YearMonth }).IsUnique(); builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
