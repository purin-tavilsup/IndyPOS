using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreIdToProductAndStoreSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_store_setting",
                table: "store_setting");

            migrationBuilder.DropIndex(
                name: "IX_product_barcode",
                table: "product");

            migrationBuilder.AddColumn<string>(
                name: "store_id",
                table: "store_setting",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "store_id",
                table: "product",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_store_setting",
                table: "store_setting",
                columns: new[] { "store_id", "key" });

            migrationBuilder.CreateIndex(
                name: "IX_store_setting_store_id",
                table: "store_setting",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_store_id",
                table: "product",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_store_id_barcode",
                table: "product",
                columns: new[] { "store_id", "barcode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_store_setting",
                table: "store_setting");

            migrationBuilder.DropIndex(
                name: "IX_store_setting_store_id",
                table: "store_setting");

            migrationBuilder.DropIndex(
                name: "IX_product_store_id",
                table: "product");

            migrationBuilder.DropIndex(
                name: "IX_product_store_id_barcode",
                table: "product");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "store_setting");

            migrationBuilder.DropColumn(
                name: "store_id",
                table: "product");

            migrationBuilder.AddPrimaryKey(
                name: "PK_store_setting",
                table: "store_setting",
                column: "key");

            migrationBuilder.CreateIndex(
                name: "IX_product_barcode",
                table: "product",
                column: "barcode",
                unique: true);
        }
    }
}
