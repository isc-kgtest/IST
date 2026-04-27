using IST.Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IST.Infrastructure.Data.EntityConfigurations.AuthEntityConfigurations;

public class UserRolesEntityConfiguration : IEntityTypeConfiguration<UserRolesEntity>
{
    public void Configure(EntityTypeBuilder<UserRolesEntity> builder)
    {
        builder.ToTable("user_roles", "access_control"); // Таблица "user_roles" в схеме "access_control"

        builder.HasKey(e => e.Id); // Первичный ключ Id

        // Убрали UlidToGuidConverter, так как свойство уже имеет тип Guid
        builder.Property(e => e.Id)
            .HasColumnType("uuid");

        // Настройка составного уникального индекса для UserId и RoleId
        // Предотвращает выдачу одной и той же роли пользователю несколько раз.
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" IS FALSE"); // Фильтр для активных записей;

        // Внешние ключи: убираем конвертеры, оставляем только тип колонки
        builder.Property(e => e.UserId)
            .HasColumnType("uuid");

        builder.Property(e => e.RoleId)
            .HasColumnType("uuid");

        // Заметка: так как DateTime в C# — значимый тип (не nullable), 
        // EF Core сделает их обязательными по умолчанию. 
        // Вызов IsRequired() здесь не обязателен, но полезен для самодокументирования кода.
        builder.Property(e => e.StartDate)
            .IsRequired();

        builder.Property(e => e.EndDate);
    }
}