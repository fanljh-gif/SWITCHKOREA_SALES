using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesManagement.Core.Models;

namespace SalesManagement.Postgres.Configurations;

public sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ConfigureEntity("import_batches"); builder.Property(x => x.Kind).HasConversion<string>(); builder.Property(x => x.Status).HasConversion<string>(); builder.Property(x => x.FileName).HasMaxLength(260); builder.HasOne<User>().WithMany().HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ConfigureEntity("audit_entries"); builder.Property(x => x.Target).HasMaxLength(200); builder.Property(x => x.Action).HasMaxLength(100); builder.Property(x => x.BeforeJson).HasColumnType("jsonb"); builder.Property(x => x.AfterJson).HasColumnType("jsonb"); builder.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
