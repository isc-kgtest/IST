using IST.Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IST.Infrastructure.AppDbContext.EntityConfigurations.AuthEntityConfigurations;

public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users", "access_control"); // Таблица "users" в схеме "access_control"

        builder.HasKey(e => e.Id); // Первичный ключ Id

        // Применение типа uuid для Id
        builder.Property(e => e.Id)
            .HasColumnType("uuid");

        // Заменили UserName на Login, так как именно он есть в UserEntity
        builder.Property(e => e.Login)
            .HasMaxLength(50)
            .IsRequired();

        // Делаем Login уникальным для активных пользователей
        builder.HasIndex(e => e.Login)
            .IsUnique()
            .HasFilter("\"IsDeleted\" IS FALSE");

        builder.Property(e => e.FullName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.Password)
            .HasMaxLength(256)
            .IsUnicode(false) // VARCHAR вместо NVARCHAR
            .IsRequired();

        builder.Property(e => e.PasswordExpiryDate)
            .IsRequired();

        // --- Дополнительные полезные настройки ---
        // Желательно ограничить длину остальных строковых полей, чтобы избежать nvarchar(max)
        builder.Property(e => e.Surname).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Patronymic).HasMaxLength(100); // Nullable, поэтому IsRequired() не ставим
        builder.Property(e => e.EMail).HasMaxLength(150).IsRequired();
        builder.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Position).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Department).HasMaxLength(100).IsRequired();

        // Настройка отношения "один-ко-многим" (User -> UserRoles)
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}