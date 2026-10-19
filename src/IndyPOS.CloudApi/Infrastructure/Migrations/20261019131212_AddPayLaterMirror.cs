using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.CloudApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayLaterMirror : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayLaters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "The store's own id for this debt, so it can be traced back to the till."),
                    StoreId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "The store this debt belongs to."),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false, comment: "The credit sale (Invoices.Id). A v3 debt's invoice may be missing from Invoices if its bulk upload was skipped."),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false, comment: "The sale's PayLater payment. A v3 debt's payment may be missing if its bulk upload was skipped."),
                    CustomerName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Who owes, as the cashier typed it at the sale."),
                    PayLaterAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, comment: "The debt taken at the sale, baht."),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, comment: "Repaid so far, baht. For a v3 debt this includes repayments made on v3, which have no rows."),
                    OwedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, computedColumnSql: "\"PayLaterAmount\" - \"PaidAmount\"", stored: true, comment: "PayLaterAmount − PaidAmount, computed by the database. Negative if overpaid."),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, comment: "True once the debt is paid in full."),
                    LegacyPaymentId = table.Column<int>(type: "integer", nullable: true, comment: "The v3 PaymentId of a debt migrated from v3; null for a v4 debt."),
                    IsFromV3 = table.Column<bool>(type: "boolean", nullable: false, computedColumnSql: "\"LegacyPaymentId\" IS NOT NULL", stored: true, comment: "True for a debt migrated from v3, computed by the database from LegacyPaymentId."),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the debt was taken, UTC."),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the debt last changed (a repayment), UTC. The newest version wins."),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the latest version of this debt reached the cloud, UTC.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayLaters", x => x.Id);
                },
                comment: "Every ลงบัญชี (PayLater) debt of a store, one row per credit sale, mirrored from StoreHub with its latest totals. v3 debts are included (IsFromV3). Not the cash drawer's DebtRepayments, which record cash entering the drawer.");

            migrationBuilder.CreateTable(
                name: "PayLaterRepayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "The store's own id for this repayment, so it can be traced back to the till."),
                    StoreId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "The store the repayment was taken at."),
                    PayLaterId = table.Column<Guid>(type: "uuid", nullable: false, comment: "The debt repaid (PayLaters.Id)."),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, comment: "Baht."),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "The store's day (Asia/Bangkok calendar date) the repayment was taken."),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the repayment was taken, UTC."),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "The cashier who took it, as Users.Id. A store's own seeded admin has no Users row."),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the repayment reached the cloud, UTC.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayLaterRepayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayLaterRepayments_PayLaters_PayLaterId",
                        column: x => x.PayLaterId,
                        principalTable: "PayLaters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Every repayment taken on a store's ลงบัญชี screen (insert-only audit), with the cashier. Sum per PayLaterId for v4 repayments; v3 debts carry no repayment history, only PaidAmount. Not the cash drawer's DebtRepayments, which record cash entering the drawer.");

            migrationBuilder.CreateIndex(
                name: "IX_PayLaterRepayments_PayLaterId",
                table: "PayLaterRepayments",
                column: "PayLaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PayLaterRepayments_StoreId_BusinessDate",
                table: "PayLaterRepayments",
                columns: new[] { "StoreId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PayLaters_StoreId_IsCompleted",
                table: "PayLaters",
                columns: new[] { "StoreId", "IsCompleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayLaterRepayments");

            migrationBuilder.DropTable(
                name: "PayLaters");
        }
    }
}
