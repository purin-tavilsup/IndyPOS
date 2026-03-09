using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// EF Core DbContext for Cloud database.
/// </summary>
public class CloudDbContext : DbContext
{
    public CloudDbContext(DbContextOptions<CloudDbContext> options) : base(options) { }

    // Inbox - raw events from stores
    public DbSet<SyncedEventEntity> SyncedEvents => Set<SyncedEventEntity>();

    // Domain entities - materialized from events
    public DbSet<CloudInvoice> Invoices => Set<CloudInvoice>();
    public DbSet<CloudInvoiceLine> InvoiceLines => Set<CloudInvoiceLine>();
    public DbSet<CloudPayment> Payments => Set<CloudPayment>();
    public DbSet<CloudInventoryMovement> InventoryMovements => Set<CloudInventoryMovement>();

    // Idempotency - tracks processed events
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SyncedEventEntity (inbox)
        modelBuilder.Entity<SyncedEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EventId).IsUnique();
            entity.HasIndex(e => new { e.StoreId, e.EventType });
            entity.HasIndex(e => e.ProcessedAtUtc);
            entity.Property(e => e.EventType).HasMaxLength(100);
        });

        // CloudInvoice
        modelBuilder.Entity<CloudInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StoreId);
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.Property(e => e.StoreId).HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
        });

        // CloudInvoiceLine
        modelBuilder.Entity<CloudInvoiceLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceId);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.Lines)
                  .HasForeignKey(e => e.InvoiceId);
        });

        // CloudPayment
        modelBuilder.Entity<CloudPayment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceId);
            entity.Property(e => e.Method).HasMaxLength(50);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.Payments)
                  .HasForeignKey(e => e.InvoiceId);
        });

        // CloudInventoryMovement
        modelBuilder.Entity<CloudInventoryMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StoreId, e.ProductId });
            entity.HasIndex(e => e.ReferenceId);
            entity.Property(e => e.StoreId).HasMaxLength(50);
            entity.Property(e => e.Reason).HasMaxLength(50);
        });

        // ProcessedEvent - for idempotency
        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.HasIndex(e => new { e.StoreId, e.EventType });
            entity.Property(e => e.StoreId).HasMaxLength(50);
            entity.Property(e => e.EventType).HasMaxLength(100);
        });
    }
}
