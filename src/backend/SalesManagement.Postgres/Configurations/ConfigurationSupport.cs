using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesManagement.Core.Models;

namespace SalesManagement.Postgres.Configurations;

internal static class ConfigurationSupport
{
    public static void ConfigureEntity<T>(this EntityTypeBuilder<T> builder, string table) where T : EntityBase
    {
        builder.ToTable(table);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
    }

    public static string MonthToString(YearMonth month) => month.ToString();
    public static YearMonth StringToMonth(string value)
    {
        var parts = value.Split('-'); return new YearMonth(int.Parse(parts[0]), int.Parse(parts[1]));
    }
}
