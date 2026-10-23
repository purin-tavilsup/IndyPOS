using IndyPOS.Cloud.Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Cloud.Auth.Data;

/// <summary>Auth's own database: OpenIddict's clients and tokens, and the stores' client records.</summary>
public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<StoreClient> StoreClients => Set<StoreClient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoreClient>(entity =>
        {
            entity.ToTable("StoreClients", t => t.HasComment(
                "Each store's OpenIddict client and whether it may get a token. Disabling is final; a store that comes back is registered again."));
            entity.HasKey(e => e.StoreId);
            entity.HasIndex(e => e.ClientId).IsUnique();
            entity.Property(e => e.StoreId).HasMaxLength(50).HasComment("The store, as CloudApi registered it.");
            entity.Property(e => e.ClientId).HasMaxLength(100).HasComment("The OpenIddict client id the store authenticates as.");
            entity.Property(e => e.Enabled).HasComment("False once the store is deactivated; a disabled store gets no token.");
            entity.Property(e => e.CreatedAtUtc).HasComment("When the client was created, UTC.");
            entity.Property(e => e.DisabledAtUtc).HasComment("When the store was deactivated, UTC.");
            entity.Property(e => e.LastAuthenticatedAtUtc).HasComment("When the store last got a token, UTC.");
        });

        modelBuilder.UseOpenIddict();
        base.OnModelCreating(modelBuilder);
    }
}
