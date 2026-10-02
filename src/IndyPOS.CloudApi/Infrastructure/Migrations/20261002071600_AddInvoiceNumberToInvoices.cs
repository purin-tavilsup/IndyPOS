using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.CloudApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceNumberToInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "InvoiceNumber",
                table: "Invoices",
                type: "bigint",
                nullable: true,
                comment: "Bill number printed on the receipt, per store; v3 history keeps its v3 number. NULL for a sale synced before bill numbers existed.");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_StoreId_InvoiceNumber",
                table: "Invoices",
                columns: new[] { "StoreId", "InvoiceNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_StoreId_InvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "Invoices");
        }
    }
}
