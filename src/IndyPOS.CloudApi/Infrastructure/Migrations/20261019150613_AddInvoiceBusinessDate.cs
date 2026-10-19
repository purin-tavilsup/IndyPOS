using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.CloudApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceBusinessDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "BusinessDate",
                table: "Invoices",
                type: "date",
                nullable: true,
                comment: "The store's day the sale belongs to (Asia/Bangkok calendar date), as StoreHub decided it. NULL only for a sale synced before StoreHub or the migrator sent it; CashDrawerDays then uses its UTC date.");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_StoreId_BusinessDate",
                table: "Invoices",
                columns: new[] { "StoreId", "BusinessDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_StoreId_BusinessDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BusinessDate",
                table: "Invoices");
        }
    }
}
