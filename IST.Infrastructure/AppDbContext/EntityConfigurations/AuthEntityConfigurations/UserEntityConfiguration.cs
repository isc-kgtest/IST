using IST.Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IST.Infrastructure.AppDbContext.EntityConfigurations.AuthEntityConfigurations;

public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users", "access_control");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnType("uuid");

        // FullName — вычисляемое свойство, в БД не сохраняется
        builder.Ignore(e => e.FullName);

        // ФИО
        builder.Property(e => e.Surname).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Patronymic).HasMaxLength(100);

        // Организационные данные
        builder.Property(e => e.Position).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Department).HasMaxLength(100);
        builder.Property(e => e.OrganizationId).HasColumnType("uuid");

        // Контакты
        builder.Property(e => e.EMail).HasMaxLength(150).IsRequired();
        builder.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();

        // Учётные данные
        builder.Property(e => e.Login).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Password)
            .HasMaxLength(256)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(e => e.PasswordExpiryDate).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        // ОЭЦП
        builder.Property(e => e.CertificateThumbprint).HasMaxLength(256);

        // Аудит (из BaseEntity)
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // === Индексы ===

        // Уникальный Login среди неудалённых
        builder.HasIndex(e => e.Login)
            .IsUnique()
            .HasFilter("\"is_deleted\" = false")
            .HasDatabaseName("ix_users_login_unique");

        // Уникальный Email среди неудалённых (требование ТЗ)
        builder.HasIndex(e => e.EMail)
            .IsUnique()
            .HasFilter("\"is_deleted\" = false")
            .HasDatabaseName("ix_users_email_unique");

        // Для фильтров по организации
        builder.HasIndex(e => e.OrganizationId)
            .HasDatabaseName("ix_users_organization");

        // Для фильтров «активные пользователи»
        builder.HasIndex(e => e.IsActive)
            .HasFilter("\"is_deleted\" = false")
            .HasDatabaseName("ix_users_is_active");

        // === Связи ===
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}