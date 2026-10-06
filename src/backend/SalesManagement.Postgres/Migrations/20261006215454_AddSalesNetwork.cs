using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesManagement.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesNetwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "network_companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Industry = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    LifecycleType = table.Column<string>(type: "text", nullable: false),
                    IsOem = table.Column<bool>(type: "boolean", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentOwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_network_companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_network_companies_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_network_companies_users_CurrentOwnerUserId",
                        column: x => x.CurrentOwnerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_company_assignments_network_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "network_companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_company_assignments_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company_contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BusinessRole = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MobilePhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    EmploymentStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_company_contacts_network_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "network_companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company_relationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UpstreamCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DownstreamCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_relationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_company_relationships_network_companies_DownstreamCompanyId",
                        column: x => x.DownstreamCompanyId,
                        principalTable: "network_companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_company_relationships_network_companies_UpstreamCompanyId",
                        column: x => x.UpstreamCompanyId,
                        principalTable: "network_companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "relationship_supplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_relationship_supplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_relationship_supplies_company_relationships_RelationshipId",
                        column: x => x.RelationshipId,
                        principalTable: "company_relationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_relationship_supplies_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_company_assignments_CompanyId",
                table: "company_assignments",
                column: "CompanyId",
                unique: true,
                filter: "\"IsCurrent\"");

            migrationBuilder.CreateIndex(
                name: "IX_company_assignments_CompanyId_ValidFrom",
                table: "company_assignments",
                columns: new[] { "CompanyId", "ValidFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_assignments_UserId",
                table: "company_assignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_company_contacts_CompanyId",
                table: "company_contacts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_company_relationships_DownstreamCompanyId",
                table: "company_relationships",
                column: "DownstreamCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_company_relationships_UpstreamCompanyId_DownstreamCompanyId~",
                table: "company_relationships",
                columns: new[] { "UpstreamCompanyId", "DownstreamCompanyId", "ValidFrom" },
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_network_companies_CurrentOwnerUserId",
                table: "network_companies",
                column: "CurrentOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_network_companies_CustomerId",
                table: "network_companies",
                column: "CustomerId",
                unique: true,
                filter: "\"CustomerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_relationship_supplies_ProductId",
                table: "relationship_supplies",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_relationship_supplies_RelationshipId_ProductId_ApplicationN~",
                table: "relationship_supplies",
                columns: new[] { "RelationshipId", "ProductId", "ApplicationName" },
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE company_relationships ADD CONSTRAINT "EX_company_relationships_no_overlap"
                EXCLUDE USING gist (
                    "UpstreamCompanyId" WITH =,
                    "DownstreamCompanyId" WITH =,
                    daterange("ValidFrom", COALESCE("ValidTo", 'infinity'::date), '[]') WITH &&
                ) WHERE ("Status" = 'Active');
                """);

            migrationBuilder.Sql("""
                ALTER TABLE company_assignments ADD CONSTRAINT "EX_company_assignments_no_overlap"
                EXCLUDE USING gist (
                    "CompanyId" WITH =,
                    daterange("ValidFrom", COALESCE("ValidTo", 'infinity'::date), '[]') WITH &&
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO network_companies
                    ("Id", "CompanyName", "Country", "Industry", "LifecycleType", "IsOem", "CustomerId", "CurrentOwnerUserId", "Status", "CreatedAt", "UpdatedAt", "Version")
                SELECT c."Id", c."CompanyName", c."Country", '미분류', 'Customer', FALSE, c."Id", c."OwnerUserId", c."Status", c."CreatedAt", c."UpdatedAt", 1
                FROM customers c
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO company_assignments
                    ("Id", "CompanyId", "UserId", "ValidFrom", "ValidTo", "IsCurrent", "CreatedAt", "UpdatedAt", "Version")
                SELECT n."Id", n."Id", c."OwnerUserId", (c."CreatedAt" AT TIME ZONE 'Asia/Seoul')::date, NULL, TRUE, c."CreatedAt", c."UpdatedAt", 1
                FROM customers c
                JOIN network_companies n ON n."CustomerId" = c."Id"
                ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_assignments");

            migrationBuilder.DropTable(
                name: "company_contacts");

            migrationBuilder.DropTable(
                name: "relationship_supplies");

            migrationBuilder.DropTable(
                name: "company_relationships");

            migrationBuilder.DropTable(
                name: "network_companies");
        }
    }
}
