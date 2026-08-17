using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Migrations
{
    /// <summary>
    /// Defect 8: records the legacy SQLite id every migrated row came from.
    /// </summary>
    /// <remarks>
    /// RELEASE GATE (docs/operations/upgrade-procedure.md, CLAUDE.md): every added column is
    /// NULLABLE, nothing is renamed and no type is narrowed, so this runs against the previous
    /// release's binaries — their INSERTs simply do not mention these columns.
    /// <para>
    /// It does contain two <c>DropIndex</c> calls, which look like a gate violation and are not.
    /// <c>IX_payment_invoice_id</c> and <c>IX_invoice_line_invoice_id</c> are EF's CONVENTION indexes
    /// on those foreign keys; EF suppresses a conventional FK index once a user-defined index carries
    /// that FK as its leading column, which the new <c>(invoice_id, legacy_*_id)</c> indexes do. The
    /// composite index serves every <c>WHERE invoice_id = ?</c> lookup the dropped one did, so the
    /// restored binaries keep both correctness and the same access path. No column is lost.
    /// </para>
    /// </remarks>
    public partial class AddLegacyIdsToMigratedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payment_invoice_id",
                table: "payment");

            migrationBuilder.DropIndex(
                name: "IX_invoice_line_invoice_id",
                table: "invoice_line");

            migrationBuilder.AddColumn<int>(
                name: "legacy_product_id",
                table: "product",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "legacy_payment_id",
                table: "payment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "legacy_invoice_line_id",
                table: "invoice_line",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "legacy_invoice_id",
                table: "invoice",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_store_id_legacy_product_id",
                table: "product",
                columns: new[] { "store_id", "legacy_product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_invoice_id_legacy_payment_id",
                table: "payment",
                columns: new[] { "invoice_id", "legacy_payment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_invoice_id_legacy_invoice_line_id",
                table: "invoice_line",
                columns: new[] { "invoice_id", "legacy_invoice_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_store_id_legacy_invoice_id",
                table: "invoice",
                columns: new[] { "store_id", "legacy_invoice_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_product_store_id_legacy_product_id",
                table: "product");

            migrationBuilder.DropIndex(
                name: "IX_payment_invoice_id_legacy_payment_id",
                table: "payment");

            migrationBuilder.DropIndex(
                name: "IX_invoice_line_invoice_id_legacy_invoice_line_id",
                table: "invoice_line");

            migrationBuilder.DropIndex(
                name: "IX_invoice_store_id_legacy_invoice_id",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "legacy_product_id",
                table: "product");

            migrationBuilder.DropColumn(
                name: "legacy_payment_id",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "legacy_invoice_line_id",
                table: "invoice_line");

            migrationBuilder.DropColumn(
                name: "legacy_invoice_id",
                table: "invoice");

            migrationBuilder.CreateIndex(
                name: "IX_payment_invoice_id",
                table: "payment",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_invoice_id",
                table: "invoice_line",
                column: "invoice_id");
        }
    }
}
