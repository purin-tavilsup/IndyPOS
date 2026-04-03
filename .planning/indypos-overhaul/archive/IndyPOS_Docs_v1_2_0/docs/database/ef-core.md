# EF Core Standardization

Version: 1.1.0  
Updated: 2026-02-28

## Why EF Core (vs Dapper)
- Migrations
- Strong typing + relationships
- Easier schema evolution
- Works for both local Postgres and cloud Postgres

## Example: DbContext

```csharp
public class StoreHubDbContext : DbContext
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<OutboxEvent> Outbox => Set<OutboxEvent>();

    public StoreHubDbContext(DbContextOptions<StoreHubDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>()
            .HasIndex(x => x.PublicId)
            .IsUnique();

        modelBuilder.Entity<OutboxEvent>()
            .HasKey(x => x.PublicId);
    }
}
```

## Example: entity with PublicId

```csharp
public class Invoice
{
    public long Id { get; set; }               // legacy/internal
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string StoreId { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
```

## Example: startup migration

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
    db.Database.Migrate();
}
```
