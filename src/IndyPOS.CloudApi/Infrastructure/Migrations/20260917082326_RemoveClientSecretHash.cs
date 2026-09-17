using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.CloudApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClientSecretHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Forward-only gate (docs/operations/upgrade-procedure.md) forbids drops so restored
            // binaries can still INSERT. It does NOT bind here: CloudApi has never been released, so
            // there are no previous binaries to roll back to. See
            // docs/superpowers/specs/2026-08-20-cloud-store-client-registry-design.md.
            migrationBuilder.DropColumn(
                name: "ClientSecretHash",
                table: "StoreConfigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientSecretHash",
                table: "StoreConfigs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
