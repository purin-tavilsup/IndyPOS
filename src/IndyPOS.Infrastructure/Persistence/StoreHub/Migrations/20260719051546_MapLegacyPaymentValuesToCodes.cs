using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Migrations
{
    /// <inheritdoc />
    public partial class MapLegacyPaymentValuesToCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Legacy StoreHubSaleService.MapPaymentType wrote PaymentType.MoneyTransfer as "Transfer".
            // The catalog Code for that method is "MoneyTransfer" (PaymentMethodCodes.MoneyTransfer).
            // Every other legacy value already matches its catalog Code 1:1, so this is the only
            // reconciliation needed. Idempotent: no-op if there are no legacy "Transfer" rows.
            migrationBuilder.Sql("UPDATE payment SET method = 'MoneyTransfer' WHERE method = 'Transfer';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentional no-op: this data migration is not cleanly reversible.
            // Reversing with `UPDATE payment SET method = 'Transfer' WHERE method = 'MoneyTransfer'`
            // would also clobber genuinely-new payments recorded post-migration with the catalog
            // Code "MoneyTransfer" (Task 9's string-Code storage), since both share the same stored
            // value and are indistinguishable after the fact. Rolling back this migration leaves
            // `method` values as-is; re-running Up remains safe (idempotent) if needed again.
        }
    }
}
