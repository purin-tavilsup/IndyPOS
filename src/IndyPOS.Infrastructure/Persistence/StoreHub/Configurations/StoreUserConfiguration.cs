using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class StoreUserConfiguration : IEntityTypeConfiguration<StoreUser>
{
    public void Configure(EntityTypeBuilder<StoreUser> builder)
    {
        builder.ToTable("store_user");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("id")
               .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.StoreId)
               .HasColumnName("store_id")
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(e => e.LegacyUserId)
               .HasColumnName("legacy_user_id");

        builder.Property(e => e.Username)
               .HasColumnName("username")
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(e => e.PasswordHash)
               .HasColumnName("password_hash")
               .HasMaxLength(255)
               .IsRequired();

        builder.Property(e => e.PasswordHashVersion)
               .HasColumnName("password_hash_version")
               .HasDefaultValue(2)
               .IsRequired();

        builder.Property(e => e.FirstName)
               .HasColumnName("first_name")
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(e => e.LastName)
               .HasColumnName("last_name")
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(e => e.RoleId)
               .HasColumnName("role_id")
               .IsRequired();

        builder.Property(e => e.IsActive)
               .HasColumnName("is_active")
               .HasDefaultValue(true)
               .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
               .HasColumnName("created_at_utc")
               .IsRequired();

        builder.Property(e => e.LastModifiedAtUtc)
               .HasColumnName("last_modified_at_utc")
               .IsRequired();

        builder.Property(e => e.LastLoginAtUtc)
               .HasColumnName("last_login_at_utc");

        builder.Property(e => e.CloudUserId)
               .HasColumnName("cloud_user_id");

        // Indexes
        builder.HasIndex(e => e.LegacyUserId)
               .IsUnique()
               .HasFilter("legacy_user_id > 0"); // Only unique when set (not 0)

        builder.HasIndex(e => new { e.StoreId, e.Username })
               .IsUnique();

        builder.HasIndex(e => e.StoreId);
    }
}
