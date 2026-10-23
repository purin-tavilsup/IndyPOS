using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.CloudApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreSyncHealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreHeartbeats",
                columns: table => new
                {
                    StoreId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "The store, from its token."),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the cloud received this heartbeat, UTC: the store's last-seen time."),
                    StoreHubVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "The StoreHub version that sent it."),
                    PendingCount = table.Column<int>(type: "integer", nullable: false, comment: "Outbox events still waiting to reach the cloud."),
                    FailedCount = table.Column<int>(type: "integer", nullable: false, comment: "Outbox events the cloud refused, parked at the store until a resend."),
                    OldestPendingCreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When the oldest waiting event was created, UTC; null when none is waiting."),
                    FailedEventsJson = table.Column<string>(type: "jsonb", nullable: true, comment: "Up to 50 of the oldest Failed events: [{eventId, eventType, failedAtUtc, reason}]; null when none.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreHeartbeats", x => x.StoreId);
                },
                comment: "Each store's latest heartbeat from StoreHub (every 2 minutes while it runs), one row per store, overwritten. No history. A store PC is usually off at night, so an old ReceivedAtUtc is not by itself a fault.");

            migrationBuilder.CreateTable(
                name: "SyncRejections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "This row's id."),
                    StoreId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "The store whose token sent the refused request."),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true, comment: "The refused outbox event; null when the batch named no event that could be read."),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "The refused event's type, when it could be read."),
                    StatusCode = table.Column<int>(type: "integer", nullable: false, comment: "The HTTP status the cloud answered: 400 or 403."),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "The cloud's refusal code and detail."),
                    FirstRejectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When this event was first refused, UTC."),
                    LastRejectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When this event was last refused, UTC."),
                    Count = table.Column<int>(type: "integer", nullable: false, comment: "How many times this event was refused."),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When the cloud later accepted this event, UTC; null while unresolved.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncRejections", x => x.Id);
                },
                comment: "Every refusal the cloud gave on /sync/events (400 malformed batch, 403 wrong store), one row per store and event. Resolved when the same event is later accepted; resolved rows stay as history.");

            migrationBuilder.CreateTable(
                name: "SyncResendRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "This request's id, which the store reports back."),
                    StoreId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "The store asked to resend."),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true, comment: "The one event to resend; null for every Failed event the store has."),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the admin asked, UTC."),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When the store reported it done, UTC; null while open."),
                    MovedEventIdsJson = table.Column<string>(type: "jsonb", nullable: true, comment: "The events the store moved back to Pending: [guid]."),
                    IgnoredEventIdsJson = table.Column<string>(type: "jsonb", nullable: true, comment: "Asked-for events the store did not have as Failed: [guid].")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncResendRequests", x => x.Id);
                },
                comment: "An admin's requests that a store resend its Failed outbox events. The store picks a request up in a heartbeat reply and reports it done in its next heartbeat.");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRejections_StoreId_EventId",
                table: "SyncRejections",
                columns: new[] { "StoreId", "EventId" },
                unique: true,
                filter: "\"EventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRejections_StoreId_ResolvedAtUtc",
                table: "SyncRejections",
                columns: new[] { "StoreId", "ResolvedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncResendRequests_StoreId_CompletedAtUtc",
                table: "SyncResendRequests",
                columns: new[] { "StoreId", "CompletedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreHeartbeats");

            migrationBuilder.DropTable(
                name: "SyncRejections");

            migrationBuilder.DropTable(
                name: "SyncResendRequests");
        }
    }
}
