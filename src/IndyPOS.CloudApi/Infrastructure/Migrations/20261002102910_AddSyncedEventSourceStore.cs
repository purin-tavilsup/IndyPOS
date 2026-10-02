using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.CloudApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncedEventSourceStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceStoreId",
                table: "SyncedEvents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "The store that sent this event: the store_id of the ingest token, already matched against the payload's StoreId. NULL for events ingested before this column existed (not counted for any store). Use this, not StoreId, which is 0 for every real store.");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedEvents_SourceStoreId",
                table: "SyncedEvents",
                column: "SourceStoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncedEvents_SourceStoreId",
                table: "SyncedEvents");

            migrationBuilder.DropColumn(
                name: "SourceStoreId",
                table: "SyncedEvents");
        }
    }
}
