using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.CloudApi.Infrastructure.Migrations
{
    /// <summary>
    /// CashDrawerDays: the till's drawer balance per store per day, derived live from rows the cloud already
    /// mirrors (spec 2026-10-19 §3). The rules repeat CashDrawerCalculator's in SQL and are pinned to it by
    /// CashDrawerDaysTests' parity test. Postgres refuses to alter a column a view reads, so a later migration
    /// that changes one of these columns must drop the view first and create it again.
    /// </summary>
    public partial class AddCashDrawerDaysView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(CreateView);
            migrationBuilder.Sql(Comments);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP VIEW IF EXISTS "CashDrawerDays";""");
        }

        // A day is any (StoreId, BusinessDate) with a sale or a drawer row, deleted ones included.
        // Cash kept per bill: its Cash payments minus the change (payments - total, never below 0, none for a refund).
        private const string CreateView = """
            CREATE VIEW "CashDrawerDays" AS
            WITH bills AS (
                SELECT i."StoreId",
                       COALESCE(i."BusinessDate", (i."CreatedAtUtc" AT TIME ZONE 'UTC')::date) AS "BusinessDate",
                       COALESCE(SUM(p."Amount") FILTER (WHERE lower(p."Method") = 'cash'), 0)
                       - CASE WHEN i."TotalAmount" < 0 THEN 0
                              ELSE GREATEST(COALESCE(SUM(p."Amount"), 0) - i."TotalAmount", 0) END AS cash_kept
                FROM "Invoices" i
                LEFT JOIN "Payments" p ON p."InvoiceId" = i."Id"
                GROUP BY i."Id", i."StoreId", i."BusinessDate", i."CreatedAtUtc", i."TotalAmount"
            ),
            sales AS (
                SELECT "StoreId", "BusinessDate", SUM(cash_kept) AS total FROM bills GROUP BY "StoreId", "BusinessDate"
            ),
            repaid AS (
                SELECT "StoreId", "BusinessDate", SUM("Amount") AS total FROM "DebtRepayments"
                WHERE NOT "IsDeleted" GROUP BY "StoreId", "BusinessDate"
            ),
            floats AS (
                SELECT "StoreId", "BusinessDate", SUM("Amount") AS total FROM "CashFloats"
                WHERE NOT "IsDeleted" GROUP BY "StoreId", "BusinessDate"
            ),
            payouts AS (
                SELECT "StoreId", "BusinessDate", SUM("Amount") AS total FROM "CashPayouts"
                WHERE NOT "IsDeleted" GROUP BY "StoreId", "BusinessDate"
            ),
            latest_count AS (
                SELECT DISTINCT ON ("StoreId", "BusinessDate")
                       "StoreId", "BusinessDate", "CreatedAtUtc",
                       1000 * "BankNote1000Count" + 500 * "BankNote500Count" + 100 * "BankNote100Count"
                       + 50 * "BankNote50Count" + 20 * "BankNote20Count" + 10 * "Coin10Count"
                       + 5 * "Coin5Count" + 2 * "Coin2Count" + "Coin1Count" AS total
                FROM "CashCounts"
                ORDER BY "StoreId", "BusinessDate", "CreatedAtUtc" DESC, "Id" DESC
            ),
            days AS (
                SELECT "StoreId", "BusinessDate" FROM bills
                UNION SELECT "StoreId", "BusinessDate" FROM "DebtRepayments"
                UNION SELECT "StoreId", "BusinessDate" FROM "CashFloats"
                UNION SELECT "StoreId", "BusinessDate" FROM "CashPayouts"
                UNION SELECT "StoreId", "BusinessDate" FROM "CashCounts"
            ),
            figures AS (
                SELECT d."StoreId", d."BusinessDate",
                       COALESCE(s.total, 0) AS cash_sales, COALESCE(r.total, 0) AS repaid,
                       COALESCE(f.total, 0) AS floats, COALESCE(po.total, 0) AS payouts,
                       c.total AS counted, c."CreatedAtUtc" AS counted_at
                FROM days d
                LEFT JOIN sales s ON s."StoreId" = d."StoreId" AND s."BusinessDate" = d."BusinessDate"
                LEFT JOIN repaid r ON r."StoreId" = d."StoreId" AND r."BusinessDate" = d."BusinessDate"
                LEFT JOIN floats f ON f."StoreId" = d."StoreId" AND f."BusinessDate" = d."BusinessDate"
                LEFT JOIN payouts po ON po."StoreId" = d."StoreId" AND po."BusinessDate" = d."BusinessDate"
                LEFT JOIN latest_count c ON c."StoreId" = d."StoreId" AND c."BusinessDate" = d."BusinessDate"
            )
            SELECT "StoreId",
                   "BusinessDate",
                   CAST(cash_sales AS numeric(18,2)) AS "CashSalesTotal",
                   CAST(repaid AS numeric(18,2)) AS "DebtRepaidTotal",
                   CAST(floats AS numeric(18,2)) AS "CashFloatsTotal",
                   CAST(payouts AS numeric(18,2)) AS "PayoutsTotal",
                   CAST(cash_sales + repaid + floats - payouts AS numeric(18,2)) AS "ExpectedCash",
                   CAST(counted AS numeric(18,2)) AS "CountedCash",
                   CAST(counted - (cash_sales + repaid + floats - payouts) AS numeric(18,2)) AS "CashDifference",
                   counted_at AS "LatestCountAtUtc"
            FROM figures;
            """;

        private const string Comments = """
            COMMENT ON VIEW "CashDrawerDays" IS 'One row per store per day that had a sale or drawer entry: the till''s expected cash, the latest count and the difference (ขาด < 0, เกิน > 0, พอดี = 0). Derived live from Invoices, Payments, CashPayouts, CashFloats, DebtRepayments and CashCounts with the till''s rules; never edited. ลงบัญชี repayments (PayLaterRepayments) are not drawer cash and are not here.';
            COMMENT ON COLUMN "CashDrawerDays"."StoreId" IS 'The store.';
            COMMENT ON COLUMN "CashDrawerDays"."BusinessDate" IS 'The store''s day (Asia/Bangkok calendar date). A sale with no BusinessDate (older or v3 bulk sales) uses its UTC date.';
            COMMENT ON COLUMN "CashDrawerDays"."CashSalesTotal" IS 'Cash the day''s sales left in the drawer, baht: each bill''s Cash payments minus its change (refunds give no change and count negative). Other methods and ลงบัญชี add nothing.';
            COMMENT ON COLUMN "CashDrawerDays"."DebtRepaidTotal" IS 'Cash customers paid into the drawer for debt (ลูกค้าชำระหนี้, DebtRepayments), baht, deleted entries left out.';
            COMMENT ON COLUMN "CashDrawerDays"."CashFloatsTotal" IS 'Change float put into the drawer (เงินทอน), baht, deleted entries left out.';
            COMMENT ON COLUMN "CashDrawerDays"."PayoutsTotal" IS 'Money taken out of the drawer (รายจ่าย), baht, deleted entries left out.';
            COMMENT ON COLUMN "CashDrawerDays"."ExpectedCash" IS 'What should be in the drawer, baht: CashSalesTotal + DebtRepaidTotal + CashFloatsTotal - PayoutsTotal. The till shows the same figure.';
            COMMENT ON COLUMN "CashDrawerDays"."CountedCash" IS 'The latest cash count of the day, baht; null until the day is counted.';
            COMMENT ON COLUMN "CashDrawerDays"."CashDifference" IS 'CountedCash - ExpectedCash, baht: negative = ขาด (short), positive = เกิน (over), 0 = พอดี (balanced). Null until counted.';
            COMMENT ON COLUMN "CashDrawerDays"."LatestCountAtUtc" IS 'When the count used here was taken, UTC; null until counted.';
            """;
    }
}
