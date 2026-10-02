using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.CloudApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceReprints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceReprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "The store's reprint id (invoice_reprint.id on the till)."),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false, comment: "The reprinted bill; Invoices.Id."),
                    StoreId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "The store that reprinted the bill."),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "The user who pressed reprint."),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When reprint was pressed, UTC."),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When this row reached the cloud, UTC.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceReprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceReprints_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Every bill reprint a store requested (insert-only audit). One row per press of reprint; a failed print still counts.");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReprints_InvoiceId",
                table: "InvoiceReprints",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReprints_StoreId_CreatedAtUtc",
                table: "InvoiceReprints",
                columns: new[] { "StoreId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceReprints");
        }
    }
}
