using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Migrations
{
    /// <summary>
    /// Adds the bill number. Hand-written rather than generated because the ORDER is the point
    /// (spec §4): legacy rows keep their v3 number, setval moves the sequence past the highest one,
    /// and only then do v4-native rows draw sequence values, oldest first. The default and NOT NULL
    /// are attached last, once every existing row has a number.
    /// </summary>
    /// <remarks>
    /// Forward-only: the column is NOT NULL but has a default, so the previous release's INSERTs,
    /// which never name it, still succeed after an installer rollback.
    /// </remarks>
    public partial class AddInvoiceNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SEQUENCE invoice_number_seq AS bigint START WITH 1 INCREMENT BY 1 NO CYCLE;");

            migrationBuilder.Sql("ALTER TABLE invoice ADD COLUMN invoice_number bigint NULL;");

            // 1. v3 bills keep their v3 number.
            migrationBuilder.Sql("UPDATE invoice SET invoice_number = legacy_invoice_id WHERE legacy_invoice_id IS NOT NULL;");

            // 2. Move the sequence past the highest number. HAVING skips the call on an empty
            //    table, where MAX is NULL and setval would fail.
            migrationBuilder.Sql("SELECT setval('invoice_number_seq', MAX(invoice_number)) FROM invoice HAVING MAX(invoice_number) IS NOT NULL;");

            // 3. v4-native rows, oldest first. A loop, because the order nextval() is evaluated in
            //    an UPDATE ... FROM is not guaranteed. Native rows are few (no till runs v4 yet).
            migrationBuilder.Sql("""
                DO $$
                DECLARE native record;
                BEGIN
                    FOR native IN SELECT id FROM invoice WHERE invoice_number IS NULL ORDER BY created_utc, id LOOP
                        UPDATE invoice SET invoice_number = nextval('invoice_number_seq') WHERE id = native.id;
                    END LOOP;
                END $$;
                """);

            migrationBuilder.Sql("ALTER TABLE invoice ALTER COLUMN invoice_number SET DEFAULT nextval('invoice_number_seq');");
            migrationBuilder.Sql("ALTER TABLE invoice ALTER COLUMN invoice_number SET NOT NULL;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_invoice_store_id_invoice_number\" ON invoice (store_id, invoice_number);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_invoice_store_id_invoice_number",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "invoice_number",
                table: "invoice");

            migrationBuilder.DropSequence(
                name: "invoice_number_seq");
        }
    }
}
