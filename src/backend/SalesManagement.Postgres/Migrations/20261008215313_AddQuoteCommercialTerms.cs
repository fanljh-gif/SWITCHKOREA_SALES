using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesManagement.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteCommercialTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryTerms",
                table: "quote_revisions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "quote_revisions",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "quote_revisions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryTerms",
                table: "quote_revisions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "quote_revisions");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "quote_revisions");
        }
    }
}
