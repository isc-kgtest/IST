using IST.Core.Entities.Auth;
using IST.Core.Entities.Organization;
using Microsoft.EntityFrameworkCore;

namespace IST.Infrastructure.Data;

/// <summary>
/// Идемпотентный сидер прав и базовых типов узлов оргструктуры.
/// Вызывается из <c>Program.cs</c> после <c>Database.Migrate()</c>.
/// </summary>
public static class SecuritySeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await SeedPermissionsAsync(db, ct);
        await EnsureAdminHasAllPermissionsAsync(db, ct);
        await SeedDefaultNodeTypesAsync(db, ct);
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

    /// <summary>
    /// Создаёт типовой набор типов узлов оргструктуры при первом старте.
    /// После этого админ может править их через UI.
    /// </summary>
    private static async Task SeedDefaultNodeTypesAsync(AppDbContext db, CancellationToken ct)
    {
        var defaults = new (string Code, string Name, int Level, int SortOrder, string Icon, string? Description)[]
        {
            ("country",     "Страна",            0, 0,  "Public",            null),
            ("region",      "Область",           1, 10, "Map",               null),
            ("district",    "Район",             2, 20, "LocationCity",      null),
            ("city",        "Город",             3, 30, "LocationCity",      null),
            ("locality",    "Населённый пункт",  3, 40, "Place",             null),
            ("ministry",    "Министерство",      1, 50, "AccountBalance",    null),
            ("department",  "Управление",        2, 60, "Apartment",         null),
            ("unit",        "Отдел",             3, 70, "Groups",            null),
            ("organization","Организация",       3, 80, "Business",          null),
        };

        var existing = await db.OrganizationNodeTypes
            .Select(t => t.Code)
            .ToListAsync(ct);

        var missing = defaults
            .Where(d => !existing.Contains(d.Code, StringComparer.OrdinalIgnoreCase))
            .Select(d => new OrganizationNodeTypeEntity
            {
                Code = d.Code,
                Name = d.Name,
                Level = d.Level,
                SortOrder = d.SortOrder,
                Icon = d.Icon,
                Description = d.Description,
            })
            .ToList();

        if (missing.Count == 0)
            return;

        await db.OrganizationNodeTypes.AddRangeAsync(missing, ct);
        await db.SaveChangesAsync(ct);
    }
}
