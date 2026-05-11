using IST.Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace IST.Infrastructure.Data;

/// <summary>
/// Идемпотентный сидер прав: добавляет в таблицу <c>permissions</c> все коды из
/// <see cref="Permissions.All"/> и привязывает их к роли <c>admin</c>.
/// Вызывается из <c>Program.cs</c> после <c>Database.Migrate()</c>.
/// </summary>
public static class SecuritySeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await SeedPermissionsAsync(db, ct);
        await EnsureAdminHasAllPermissionsAsync(db, ct);
    }

    private static async Task SeedPermissionsAsync(AppDbContext db, CancellationToken ct)
    {
        var existingCodes = await db.Permissions
            .Select(p => p.Code)
            .ToListAsync(ct);

        var missing = Permissions.All
            .Where(p => !existingCodes.Contains(p.Code, StringComparer.OrdinalIgnoreCase))
            .Select(p => new PermissionEntity
            {
                Code = p.Code,
                Description = p.Description,
                Category = p.Category,
            })
            .ToList();

        if (missing.Count == 0)
            return;

        await db.Permissions.AddRangeAsync(missing, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureAdminHasAllPermissionsAsync(AppDbContext db, CancellationToken ct)
    {
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "admin", ct);
        if (adminRole is null)
            return;

        var allPermissionIds = await db.Permissions.Select(p => p.Id).ToListAsync(ct);
        var existingLinks = await db.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);

        var toAdd = allPermissionIds.Except(existingLinks)
            .Select(pid => new RolePermissionEntity
            {
                RoleId = adminRole.Id,
                PermissionId = pid,
            })
            .ToList();

        if (toAdd.Count == 0)
            return;

        await db.RolePermissions.AddRangeAsync(toAdd, ct);
        await db.SaveChangesAsync(ct);
    }
}
