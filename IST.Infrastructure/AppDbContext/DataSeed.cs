using IST.Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace IST.Infrastructure.AppDbContext;

public static class DataSeed
{
    public static void DBInitializer(this ModelBuilder modelBuilder)
    {
        var adminUserId = Guid.NewGuid();
        var adminRoleId = Guid.NewGuid();
        var userRoleId = Guid.NewGuid();

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
