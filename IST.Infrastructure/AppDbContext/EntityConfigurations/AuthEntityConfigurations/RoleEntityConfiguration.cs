using IST.Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IST.Infrastructure.AppDbContext.EntityConfigurations.AuthEntityConfigurations;

public class RoleEntityConfiguration : IEntityTypeConfiguration<RoleEntity>
{
    public void Configure(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.ToTable("roles", "access_control"); // Таблица "roles" в схеме "access_control"

        builder.HasKey(e => e.Id); // Первичный ключ Id

        // Убрали конвертер, так как Id уже Guid. Оставляем только тип колонки.
        builder.Property(e => e.Id)
            .HasColumnType("uuid");

        builder.Property(e => e.Name)
            .HasMaxLength(50)
            .IsRequired();

        // Делаем Name уникальным для активных ролей
        builder.HasIndex(e => e.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" IS FALSE");

        builder.Property(e => e.Description)
            .HasMaxLength(250); // Nullable по умолчанию, IsRequired() не нужен

        // Настройка отношения "один-ко-многим" (Role -> UserRoles)
        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}