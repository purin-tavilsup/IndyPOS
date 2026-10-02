using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.CloudApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInboxRetrySchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "SyncedEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAtUtc",
                table: "SyncedEvents",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attempts",
                table: "SyncedEvents");

            migrationBuilder.DropColumn(
                name: "NextAttemptAtUtc",
                table: "SyncedEvents");
        }
    }
}
