using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Migrations
{
    /// <inheritdoc />
    public partial class ReclassifyPaymentMethodKinds : Migration
    {
        // PaymentMethodKind backing values: Standard = 1 (renamed from Permanent),
        // GovernmentCampaign = 2, Special = 3 (new).
        private const int Standard = 1;
        private const int GovernmentCampaign = 2;
        private const int Special = 3;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The seeder only INSERTs absent codes, so stores provisioned before this
            // change keep the original classification (everything non-campaign = 1).
            // Re-point the two rows whose kind changed. Cash/MoneyTransfer stay 1
            // because Standard reuses Permanent's backing value.
            // Idempotent: the kind guard makes a re-run a no-op, and an admin who has
            // since re-classified a row by hand is left alone.
            migrationBuilder.Sql(
                $"UPDATE payment_method SET kind = {Special} WHERE code = 'PayLater' AND kind = {Standard};");
            migrationBuilder.Sql(
                $"UPDATE payment_method SET kind = {GovernmentCampaign} WHERE code = 'WelfareCard' AND kind = {Standard};");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cleanly reversible, unlike the earlier payment-value migration: these rows
            // are addressed by Code, so restoring kind = 1 cannot clobber unrelated data.
            migrationBuilder.Sql(
                $"UPDATE payment_method SET kind = {Standard} WHERE code = 'PayLater' AND kind = {Special};");
            migrationBuilder.Sql(
                $"UPDATE payment_method SET kind = {Standard} WHERE code = 'WelfareCard' AND kind = {GovernmentCampaign};");
        }
    }
}
