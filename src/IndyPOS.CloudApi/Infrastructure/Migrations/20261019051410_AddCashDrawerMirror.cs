using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.CloudApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashDrawerMirror : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashCounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "The store's own id for this row, so it can be traced back to the till."),
                    StoreId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "The store this row belongs to."),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "The store's cash day (Asia/Bangkok calendar date) the row belongs to."),
                    BankNote1000Count = table.Column<int>(type: "integer", nullable: false, comment: "Number of 1000-baht banknotes counted."),
                    BankNote500Count = table.Column<int>(type: "integer", nullable: false, comment: "Number of 500-baht banknotes counted."),
                    BankNote100Count = table.Column<int>(type: "integer", nullable: false, comment: "Number of 100-baht banknotes counted."),
                    BankNote50Count = table.Column<int>(type: "integer", nullable: false, comment: "Number of 50-baht banknotes counted."),
                    BankNote20Count = table.Column<int>(type: "integer", nullable: false, comment: "Number of 20-baht banknotes counted."),
                    Coin10Count = table.Column<int>(type: "integer", nullable: false, comment: "Number of 10-baht coins counted."),
                    Coin5Count = table.Column<int>(type: "integer", nullable: false, comment: "Number of 5-baht coins counted."),
                    Coin2Count = table.Column<int>(type: "integer", nullable: false, comment: "Number of 2-baht coins counted."),
                    Coin1Count = table.Column<int>(type: "integer", nullable: false, comment: "Number of 1-baht coins counted."),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the count was taken, UTC."),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Equals CreatedAtUtc; a count is never edited."),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Who counted, as Users.Id. A store's own seeded admin has no Users row."),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the latest version of this row reached the cloud, UTC.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashCounts", x => x.Id);
                },
                comment: "Every cash count of a store's drawer (insert-only). The latest row per StoreId+BusinessDate is the count used for the cash difference; earlier rows are audit history.");

            migrationBuilder.CreateTable(
                name: "CashFloats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "The store's own id for this row, so it can be traced back to the till."),
                    StoreId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "The store this row belongs to."),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, comment: "Baht."),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "A note the cashier typed. Optional."),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "The store's cash day (Asia/Bangkok calendar date) the row belongs to."),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the entry was first saved, UTC."),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the entry last changed (edit or delete), UTC. The newest version wins."),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Who added the entry, as Users.Id. A store's own seeded admin has no Users row."),
                    LastModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Who last edited or deleted it, as Users.Id. Null if never changed."),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, comment: "True when the entry was deleted at the till. Kept for audit."),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When it was deleted, UTC. Null if not deleted."),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the latest version of this row reached the cloud, UTC.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashFloats", x => x.Id);
                },
                comment: "Money put into a store's cash drawer as change float (เงินทอน), one row per entry, mirrored from StoreHub. Rows with IsDeleted are cancelled entries kept for audit; leave them out of totals.");

            migrationBuilder.CreateTable(
                name: "CashPayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "The store's own id for this row, so it can be traced back to the till."),
                    StoreId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "The store this row belongs to."),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "General (เบ็ดเตล็ด) or Hardware; which goods the payout was for."),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, comment: "Baht."),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "What the money was for, as the cashier typed it. Optional."),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "The store's cash day (Asia/Bangkok calendar date) the row belongs to."),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the entry was first saved, UTC."),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the entry last changed (edit or delete), UTC. The newest version wins."),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Who added the entry, as Users.Id. A store's own seeded admin has no Users row."),
                    LastModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Who last edited or deleted it, as Users.Id. Null if never changed."),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, comment: "True when the entry was deleted at the till. Kept for audit."),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When it was deleted, UTC. Null if not deleted."),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the latest version of this row reached the cloud, UTC.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashPayouts", x => x.Id);
                },
                comment: "Money taken out of a store's cash drawer (รายจ่าย), one row per entry, mirrored from StoreHub. Rows with IsDeleted are cancelled entries kept for audit; leave them out of totals.");

            migrationBuilder.CreateTable(
                name: "DebtRepayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "The store's own id for this row, so it can be traced back to the till."),
                    StoreId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "The store this row belongs to."),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Who paid, as the cashier typed it."),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, comment: "Baht."),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "The store's cash day (Asia/Bangkok calendar date) the row belongs to."),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the entry was first saved, UTC."),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the entry last changed (edit or delete), UTC. The newest version wins."),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Who added the entry, as Users.Id. A store's own seeded admin has no Users row."),
                    LastModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Who last edited or deleted it, as Users.Id. Null if never changed."),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, comment: "True when the entry was deleted at the till. Kept for audit."),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When it was deleted, UTC. Null if not deleted."),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the latest version of this row reached the cloud, UTC.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtRepayments", x => x.Id);
                },
                comment: "Cash a customer paid into the drawer to settle PayLater debt (ลูกค้าชำระหนี้), mirrored from StoreHub. Not linked to a PayLater row. Rows with IsDeleted are cancelled entries kept for audit; leave them out of totals.");

            migrationBuilder.CreateIndex(
                name: "IX_CashCounts_StoreId_BusinessDate",
                table: "CashCounts",
                columns: new[] { "StoreId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CashCounts_StoreId_BusinessDate_CreatedAtUtc",
                table: "CashCounts",
                columns: new[] { "StoreId", "BusinessDate", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CashFloats_StoreId_BusinessDate",
                table: "CashFloats",
                columns: new[] { "StoreId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CashPayouts_StoreId_BusinessDate",
                table: "CashPayouts",
                columns: new[] { "StoreId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DebtRepayments_StoreId_BusinessDate",
                table: "DebtRepayments",
                columns: new[] { "StoreId", "BusinessDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashCounts");

            migrationBuilder.DropTable(
                name: "CashFloats");

            migrationBuilder.DropTable(
                name: "CashPayouts");

            migrationBuilder.DropTable(
                name: "DebtRepayments");
        }
    }
}
