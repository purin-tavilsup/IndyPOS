using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// EF Core DbContext for Cloud database.
/// Includes OpenIddict entities for OAuth2 token management.
/// </summary>
public class CloudDbContext : DbContext
{
    public CloudDbContext(DbContextOptions<CloudDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // OpenIddict uses its own entity configuration
        base.OnConfiguring(optionsBuilder);
    }

    // Inbox - raw events from stores
    public DbSet<SyncedEventEntity> SyncedEvents => Set<SyncedEventEntity>();

    // Domain entities - materialized from events
    public DbSet<CloudInvoice> Invoices => Set<CloudInvoice>();
    public DbSet<CloudInvoiceLine> InvoiceLines => Set<CloudInvoiceLine>();
    public DbSet<CloudPayment> Payments => Set<CloudPayment>();
    public DbSet<CloudInventoryMovement> InventoryMovements => Set<CloudInventoryMovement>();

    // Master data - distributed to stores
    public DbSet<CloudProduct> Products => Set<CloudProduct>();
    public DbSet<CloudStoreConfig> StoreConfigs => Set<CloudStoreConfig>();
    public DbSet<CloudUser> Users => Set<CloudUser>();

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

        // CloudProduct - master data
        modelBuilder.Entity<CloudProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Barcode);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Barcode).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Brand).HasMaxLength(100);
            entity.Property(e => e.Manufacturer).HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.GroupPrice).HasPrecision(18, 2);
        });

        // CloudStoreConfig - per-store configuration with OAuth2 credentials
        modelBuilder.Entity<CloudStoreConfig>(entity =>
        {
            entity.HasKey(e => e.StoreId);
            entity.Property(e => e.StoreId).HasMaxLength(50);
            entity.Property(e => e.StoreName).HasMaxLength(100);
            entity.Property(e => e.StoreFullName).HasMaxLength(200);
            entity.Property(e => e.AddressLine1).HasMaxLength(200);
            entity.Property(e => e.AddressLine2).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.PrinterName).HasMaxLength(100);

            // OAuth2 fields
            entity.Property(e => e.ClientId).HasMaxLength(100);
            entity.HasIndex(e => e.ClientId).IsUnique();
            entity.Property(e => e.ClientSecretHash).HasMaxLength(200);
            entity.HasIndex(e => e.IsActive);
        });

        // CloudUser - master user data distributed to stores
        modelBuilder.Entity<CloudUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StoreId);
            entity.HasIndex(e => new { e.StoreId, e.Username }).IsUnique();
            entity.HasIndex(e => e.Version);  // For efficient incremental sync
            entity.Property(e => e.StoreId).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
        });

        // OpenIddict's EF entities (application, authorization, scope, token) are
        // registered against this context by AddCore().UseDbContext<CloudDbContext>(),
        // so they must be in the model or its stores throw at runtime.
        base.OnModelCreating(modelBuilder);
        modelBuilder.UseOpenIddict();
    }
}
