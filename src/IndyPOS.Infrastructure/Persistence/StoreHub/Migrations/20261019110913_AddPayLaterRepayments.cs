using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddPayLaterRepayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "legacy_payment_id",
                table: "pay_later",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "pay_later_repayment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pay_later_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pay_later_repayment", x => x.id);
                    table.ForeignKey(
                        name: "FK_pay_later_repayment_pay_later_pay_later_id",
                        column: x => x.pay_later_id,
                        principalTable: "pay_later",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pay_later_legacy_payment_id",
                table: "pay_later",
                column: "legacy_payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_pay_later_repayment_pay_later_id",
                table: "pay_later_repayment",
                column: "pay_later_id");

            migrationBuilder.CreateIndex(
                name: "IX_pay_later_repayment_store_id_business_date",
                table: "pay_later_repayment",
                columns: new[] { "store_id", "business_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pay_later_repayment");

            migrationBuilder.DropIndex(
                name: "IX_pay_later_legacy_payment_id",
                table: "pay_later");

            migrationBuilder.DropColumn(
                name: "legacy_payment_id",
                table: "pay_later");
        }
    }
}
