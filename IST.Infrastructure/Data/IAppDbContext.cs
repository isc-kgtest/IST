using IST.Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IST.Infrastructure.Data;

public interface IAppDbContext
{
    DbSet<UserEntity> Users { get; set; }
    DbSet<RoleEntity> Roles { get; set; }
    DbSet<UserRolesEntity> UserRoles { get; set; }
    DatabaseFacade Database { get; }
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}