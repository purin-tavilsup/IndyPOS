using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddCashDrawerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cash_count",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    bank_note_1000_count = table.Column<int>(type: "integer", nullable: false),
                    bank_note_500_count = table.Column<int>(type: "integer", nullable: false),
                    bank_note_100_count = table.Column<int>(type: "integer", nullable: false),
                    bank_note_50_count = table.Column<int>(type: "integer", nullable: false),
                    bank_note_20_count = table.Column<int>(type: "integer", nullable: false),
                    coin_10_count = table.Column<int>(type: "integer", nullable: false),
                    coin_5_count = table.Column<int>(type: "integer", nullable: false),
                    coin_2_count = table.Column<int>(type: "integer", nullable: false),
                    coin_1_count = table.Column<int>(type: "integer", nullable: false),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_count", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cash_float",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    store_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_float", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cash_payout",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "General"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    store_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_payout", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "debt_repayment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    store_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_debt_repayment", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cash_count_store_id_business_date_created_utc",
                table: "cash_count",
                columns: new[] { "store_id", "business_date", "created_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_cash_float_store_id_business_date",
                table: "cash_float",
                columns: new[] { "store_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "IX_cash_payout_store_id_business_date",
                table: "cash_payout",
                columns: new[] { "store_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "IX_debt_repayment_store_id_business_date",
                table: "debt_repayment",
                columns: new[] { "store_id", "business_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cash_count");

            migrationBuilder.DropTable(
                name: "cash_float");

            migrationBuilder.DropTable(
                name: "cash_payout");

            migrationBuilder.DropTable(
                name: "debt_repayment");
        }
    }
}
