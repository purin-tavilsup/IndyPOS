using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// The ลงบัญชี mirror tables. Dashboards and AI agents read them through the schema alone, so every table
/// and column carries a comment that states the rule an agent needs (spec 2026-10-19 §3).
/// </summary>
internal static class PayLaterMirrorModel
{
    private const string NotTheDrawer = "Not the cash drawer's DebtRepayments, which record cash entering the drawer.";

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CloudPayLater>(entity =>
        {
            entity.ToTable("PayLaters", t => t.HasComment(
                $"Every ลงบัญชี (PayLater) debt of a store, one row per credit sale, mirrored from StoreHub with its latest totals. v3 debts are included (IsFromV3). {NotTheDrawer}"));
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StoreId, e.IsCompleted });
            entity.Property(e => e.Id).ValueGeneratedNever().HasComment("The store's own id for this debt, so it can be traced back to the till.");
            entity.Property(e => e.StoreId).HasMaxLength(50).HasComment("The store this debt belongs to.");
            entity.Property(e => e.InvoiceId).HasComment("The credit sale (Invoices.Id). A v3 debt's invoice may be missing from Invoices if its bulk upload was skipped.");
            entity.Property(e => e.PaymentId).HasComment("The sale's PayLater payment. A v3 debt's payment may be missing if its bulk upload was skipped.");
            entity.Property(e => e.CustomerName).HasMaxLength(500).HasComment("Who owes, as the cashier typed it at the sale.");
            entity.Property(e => e.PayLaterAmount).HasPrecision(18, 2).HasComment("The debt taken at the sale, baht.");
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2).HasComment("Repaid so far, baht. For a v3 debt this includes repayments made on v3, which have no rows.");
            entity.Property(e => e.OwedAmount).HasPrecision(18, 2)
                  .HasComputedColumnSql("\"PayLaterAmount\" - \"PaidAmount\"", stored: true)
                  .HasComment("PayLaterAmount − PaidAmount, computed by the database. Negative if overpaid.");
            entity.Property(e => e.IsCompleted).HasComment("True once the debt is paid in full.");
            entity.Property(e => e.LegacyPaymentId).HasComment("The v3 PaymentId of a debt migrated from v3; null for a v4 debt.");
            entity.Property(e => e.IsFromV3)
                  .HasComputedColumnSql("\"LegacyPaymentId\" IS NOT NULL", stored: true)
                  .HasComment("True for a debt migrated from v3, computed by the database from LegacyPaymentId.");
            entity.Property(e => e.CreatedAtUtc).HasComment("When the debt was taken, UTC.");
            entity.Property(e => e.LastModifiedAtUtc).HasComment("When the debt last changed (a repayment), UTC. The newest version wins.");
            entity.Property(e => e.SyncedAtUtc).HasComment("When the latest version of this debt reached the cloud, UTC.");
        });

        modelBuilder.Entity<CloudPayLaterRepayment>(entity =>
        {
            entity.ToTable("PayLaterRepayments", t => t.HasComment(
                $"Every repayment taken on a store's ลงบัญชี screen (insert-only audit), with the cashier. Sum per PayLaterId for v4 repayments; v3 debts carry no repayment history, only PaidAmount. {NotTheDrawer}"));
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StoreId, e.BusinessDate });
            entity.HasOne<CloudPayLater>()
                  .WithMany()
                  .HasForeignKey(e => e.PayLaterId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Id).ValueGeneratedNever().HasComment("The store's own id for this repayment, so it can be traced back to the till.");
            entity.Property(e => e.StoreId).HasMaxLength(50).HasComment("The store the repayment was taken at.");
            entity.Property(e => e.PayLaterId).HasComment("The debt repaid (PayLaters.Id).");
            entity.Property(e => e.Amount).HasPrecision(18, 2).HasComment("Baht.");
            entity.Property(e => e.BusinessDate).HasComment("The store's day (Asia/Bangkok calendar date) the repayment was taken.");
            entity.Property(e => e.CreatedAtUtc).HasComment("When the repayment was taken, UTC.");
            entity.Property(e => e.CreatedByUserId).HasComment("The cashier who took it, as Users.Id. A store's own seeded admin has no Users row.");
            entity.Property(e => e.SyncedAtUtc).HasComment("When the repayment reached the cloud, UTC.");
        });
    }
}
