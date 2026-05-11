using IST.Core.Entities.Audit;
using IST.Core.Entities.Auth;
using IST.Core.Entities.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IST.Infrastructure.Data;

public interface IAppDbContext
{
    DbSet<UserEntity> Users { get; set; }
    DbSet<RoleEntity> Roles { get; set; }
    DbSet<UserRolesEntity> UserRoles { get; set; }
    DbSet<PermissionEntity> Permissions { get; set; }
    DbSet<RolePermissionEntity> RolePermissions { get; set; }
    DbSet<SecurityAuditLogEntity> SecurityAuditLogs { get; set; }
    DbSet<OrganizationNodeTypeEntity> OrganizationNodeTypes { get; set; }
    DbSet<OrganizationNodeEntity> OrganizationNodes { get; set; }
    DatabaseFacade Database { get; }
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}