using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// The cash-drawer mirror tables. Dashboards and AI agents read them through the schema alone, so
/// every table and column carries a comment that states the rule an agent needs (spec 2026-09-20 §7).
/// </summary>
internal static class CashDrawerMirrorModel
{
    private const string DeletedRule = "Rows with IsDeleted are cancelled entries kept for audit; leave them out of totals.";

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CloudCashPayout>(entity =>
        {
            entity.ToTable("CashPayouts", t => t.HasComment(
                $"Money taken out of a store's cash drawer (รายจ่าย), one row per entry, mirrored from StoreHub. {DeletedRule}"));
            ConfigureEntry(entity);
            entity.Property(e => e.Category).HasMaxLength(20).HasComment("General (เบ็ดเตล็ด) or Hardware; which goods the payout was for.");
            entity.Property(e => e.Description).HasMaxLength(500).HasComment("What the money was for, as the cashier typed it. Optional.");
        });

        modelBuilder.Entity<CloudCashFloat>(entity =>
        {
            entity.ToTable("CashFloats", t => t.HasComment(
                $"Money put into a store's cash drawer as change float (เงินทอน), one row per entry, mirrored from StoreHub. {DeletedRule}"));
            ConfigureEntry(entity);
            entity.Property(e => e.Description).HasMaxLength(500).HasComment("A note the cashier typed. Optional.");
        });

        modelBuilder.Entity<CloudDebtRepayment>(entity =>
        {
            entity.ToTable("DebtRepayments", t => t.HasComment(
                $"Cash a customer paid into the drawer to settle PayLater debt (ลูกค้าชำระหนี้), mirrored from StoreHub. Not linked to a PayLater row. {DeletedRule}"));
            ConfigureEntry(entity);
            entity.Property(e => e.CustomerName).HasMaxLength(200).HasComment("Who paid, as the cashier typed it.");
        });

        modelBuilder.Entity<CloudCashCount>(entity =>
        {
            entity.ToTable("CashCounts", t => t.HasComment(
                "Every cash count of a store's drawer (insert-only). The latest row per StoreId+BusinessDate is the count used for the cash difference; earlier rows are audit history."));
            ConfigureRow(entity);
            entity.HasIndex(e => new { e.StoreId, e.BusinessDate, e.CreatedAtUtc });
            entity.Property(e => e.CreatedAtUtc).HasComment("When the count was taken, UTC.");
            entity.Property(e => e.LastModifiedAtUtc).HasComment("Equals CreatedAtUtc; a count is never edited.");
            entity.Property(e => e.CreatedByUserId).HasComment("Who counted, as Users.Id. A store's own seeded admin has no Users row.");
            CountComment(entity, e => e.BankNote1000Count, "1000-baht banknotes");
            CountComment(entity, e => e.BankNote500Count, "500-baht banknotes");
            CountComment(entity, e => e.BankNote100Count, "100-baht banknotes");
            CountComment(entity, e => e.BankNote50Count, "50-baht banknotes");
            CountComment(entity, e => e.BankNote20Count, "20-baht banknotes");
            CountComment(entity, e => e.Coin10Count, "10-baht coins");
            CountComment(entity, e => e.Coin5Count, "5-baht coins");
            CountComment(entity, e => e.Coin2Count, "2-baht coins");
            CountComment(entity, e => e.Coin1Count, "1-baht coins");
        });
    }

    // The columns every mirror row shares.
    private static void ConfigureRow<T>(EntityTypeBuilder<T> entity) where T : class, ICloudCashRow
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(nameof(ICloudCashRow.StoreId), "BusinessDate");
        entity.Property(e => e.Id).ValueGeneratedNever().HasComment("The store's own id for this row, so it can be traced back to the till.");
        entity.Property(e => e.StoreId).HasMaxLength(50).HasComment("The store this row belongs to.");
        entity.Property<DateOnly>("BusinessDate").HasComment("The store's cash day (Asia/Bangkok calendar date) the row belongs to.");
        entity.Property<DateTime>("SyncedAtUtc").HasComment("When the latest version of this row reached the cloud, UTC.");
    }

    // The columns payouts, floats and repayments share on top of ConfigureRow.
    private static void ConfigureEntry<T>(EntityTypeBuilder<T> entity) where T : class, ICloudCashRow
    {
        ConfigureRow(entity);
        entity.Property<decimal>("Amount").HasPrecision(18, 2).HasComment("Baht.");
        entity.Property<DateTime>("CreatedAtUtc").HasComment("When the entry was first saved, UTC.");
        entity.Property(e => e.LastModifiedAtUtc).HasComment("When the entry last changed (edit or delete), UTC. The newest version wins.");
        entity.Property<Guid>("CreatedByUserId").HasComment("Who added the entry, as Users.Id. A store's own seeded admin has no Users row.");
        entity.Property<Guid?>("LastModifiedByUserId").HasComment("Who last edited or deleted it, as Users.Id. Null if never changed.");
        entity.Property<bool>("IsDeleted").HasComment("True when the entry was deleted at the till. Kept for audit.");
        entity.Property<DateTime?>("DeletedAtUtc").HasComment("When it was deleted, UTC. Null if not deleted.");
    }

    private static void CountComment(EntityTypeBuilder<CloudCashCount> entity,
                                     System.Linq.Expressions.Expression<Func<CloudCashCount, int>> count,
                                     string what) =>
        entity.Property(count).HasComment($"Number of {what} counted.");
}
