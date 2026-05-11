using IST.Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IST.Infrastructure.Data.EntityConfigurations.AuthEntityConfigurations;

public class PermissionEntityConfiguration : IEntityTypeConfiguration<PermissionEntity>
{
    public void Configure(EntityTypeBuilder<PermissionEntity> builder)
    {
        builder.ToTable("permissions", "access_control");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnType("uuid");

        builder.Property(e => e.Code).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(250).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(50);

        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("ix_permissions_code_unique");

        builder.HasMany(p => p.RolePermissions)
            .WithOne(rp => rp.Permission)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
