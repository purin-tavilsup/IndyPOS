using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceLineLegacyDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "group_price",
                table: "invoice_line",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "note",
                table: "invoice_line",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "invoice_line",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "group_price",
                table: "invoice_line");

            migrationBuilder.DropColumn(
                name: "note",
                table: "invoice_line");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "invoice_line");
        }
    }
}
