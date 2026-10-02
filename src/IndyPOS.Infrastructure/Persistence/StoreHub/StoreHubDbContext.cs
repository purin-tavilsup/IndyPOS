using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub;

/// <summary>
/// EF Core DbContext for StoreHub PostgreSQL database.
/// </summary>
public class StoreHubDbContext : DbContext
{
    public StoreHubDbContext(DbContextOptions<StoreHubDbContext> options)
        : base(options)
    {
    }

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PayLater> PayLaters => Set<PayLater>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<StoreUser> StoreUsers => Set<StoreUser>();
    public DbSet<StoreSetting> StoreSettings => Set<StoreSetting>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<CashPayout> CashPayouts => Set<CashPayout>();
    public DbSet<CashFloat> CashFloats => Set<CashFloat>();
    public DbSet<DebtRepayment> DebtRepayments => Set<DebtRepayment>();
    public DbSet<CashCount> CashCounts => Set<CashCount>();
    public DbSet<InvoiceReprint> InvoiceReprints => Set<InvoiceReprint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(InvoiceNumberSequence.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StoreHubDbContext).Assembly);
    }
}
