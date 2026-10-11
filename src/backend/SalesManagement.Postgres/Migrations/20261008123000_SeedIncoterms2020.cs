using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SalesManagement.Postgres.Migrations;

[DbContext(typeof(SalesDbContext))]
[Migration("20261008123000_SeedIncoterms2020")]
public sealed class SeedIncoterms2020 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add missing codes only: preserve manager-maintained names, status and ordering.
        migrationBuilder.Sql("""
            INSERT INTO incoterms ("Id", "Code", "DisplayName", "Status", "SortOrder", "CreatedAt", "UpdatedAt", "Version")
            VALUES
            ('60000000-0000-0000-0000-000000000001', 'EXW', 'Ex Works', 'Active', 10, '2026-10-08T00:00:00+00:00', '2026-10-08T00:00:00+00:00', 1),
            ('60000000-0000-0000-0000-000000000002', 'FCA', 'Free Carrier', 'Active', 20, '2026-10-08T00:00:00+00:00', '2026-10-08T00:00:00+00:00', 1),
            ('60000000-0000-0000-0000-000000000003', 'CPT', 'Carriage Paid To', 'Active', 30, '2026-10-08T00:00:00+00:00', '2026-10-08T00:00:00+00:00', 1),
            ('60000000-0000-0000-0000-000000000004', 'CIP', 'Carriage and Insurance Paid To', 'Active', 40, '2026-10-08T00:00:00+00:00', '2026-10-08T00:00:00+00:00', 1),
            ('60000000-0000-0000-0000-000000000005', 'DAP', 'Delivered at Place', 'Active', 50, '2026-10-08T00:00:00+00:00', '2026-10-08T00:00:00+00:00', 1),
            ('60000000-0000-0000-0000-000000000006', 'DPU', 'Delivered at Place Unloaded', 'Active', 60, '2026-10-08T00:00:00+00:00', '2026-10-08T00:00:00+00:00', 1),
            ('60000000-0000-0000-0000-000000000007', 'DDP', 'Delivered Duty Paid', 'Active', 70, '2026-10-08T00:00:00+00:00', '2026-10-08T00:00:00+00:00', 1),
            ('60000000-0000-0000-0000-000000000008', 'FAS', 'Free Alongside Ship', 'Active', 80, '2026-10-08T00:00:00+00:00', '2026-10-08T00:00:00+00:00', 1),
            ('60000000-0000-0000-0000-000000000009', 'FOB', 'Free On Board', 'Active', 90, '2026-10-08T00:00:00+00:00', '2026-10-08T00:00:00+00:00', 1),
            ('60000000-0000-0000-0000-000000000010', 'CFR', 'Cost and Freight', 'Active', 100, '2026-10-08T00:00:00+00:00', '2026-10-08T00:00:00+00:00', 1),
            ('60000000-0000-0000-0000-000000000011', 'CIF', 'Cost Insurance and Freight', 'Active', 110, '2026-10-08T00:00:00+00:00', '2026-10-08T00:00:00+00:00', 1)
            ON CONFLICT ("Code") DO NOTHING;
            """);
    }

    // Seeded master data may have been edited or referenced. Rollback must not delete it.
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
