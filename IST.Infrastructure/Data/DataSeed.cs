using IST.Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace IST.Infrastructure.Data;

public static class DataSeed
{
    public static void DBInitializer(this ModelBuilder modelBuilder)
    {
        var adminUserId = Guid.Parse("8a92bd3a-58c3-433b-8255-8abef51421b0");
        var adminRoleId = Guid.Parse("e2c2c0d5-1c5c-482a-904b-3549cd0ebba0");
        var userRoleId = Guid.Parse("c8a2b5e3-4f91-4d9a-8b8a-7c9b8e1a2f3b");

        var createDateTimeUtcNow = new DateTime(2026, 4, 21, 10, 48, 0, DateTimeKind.Utc);

        // --- 1. Роли ---
        modelBuilder.Entity<RoleEntity>().HasData(
            new RoleEntity
            {
                Id = adminRoleId,
                Name = "admin",
                Description = "admin role",
                CreatedAt = createDateTimeUtcNow // Заменил CreateDate на CreatedAt
            }
        );

        // --- 2. Пользователи ---
        modelBuilder.Entity<UserEntity>().HasData(
            new UserEntity
            {
                Id = adminUserId,
                Login = "admin", // Заменил UserName на Login
                Surname = "Системов", // Добавил обязательные поля
                Name = "Админ",
                Patronymic = "Админович",
                Position = "Системный администратор",
                Department = "IT Отдел",
                EMail = "admin@system.local",
                PhoneNumber = "+00000000000",
                IsActive = true,
                LastDateLogin = createDateTimeUtcNow,
                OrganizationId = Guid.Empty, // Заглушка, так как поле не nullable
                Password = "$argon2id$v=19$m=32768,t=4,p=1$j8Hfb0sAcmWRKWanHyDh9A$spaN8XOINtX1M0PNUV4esKl02Fdv/2Fxr45V5aSJbfo",
                PasswordExpiryDate = createDateTimeUtcNow,
                CreatedAt = createDateTimeUtcNow
            }
        );

        // --- 3. Связь Пользователь-Роль ---
        modelBuilder.Entity<UserRolesEntity>().HasData(
            new UserRolesEntity
            {
                Id = userRoleId,
                RoleId = adminRoleId,
                UserId = adminUserId,
                StartDate = new DateTime(2026, 4, 21, 10, 48, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2035, 7, 03, 10, 48, 0, DateTimeKind.Utc),
                CreatedAt = createDateTimeUtcNow
            }
        );
    }
}
