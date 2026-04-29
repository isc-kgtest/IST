using ActualLab.Fusion.EntityFramework;
using ActualLab.Fusion.EntityFramework.Operations;
using IST.Core.Entities.Auth;
using IST.Core.Entities.BaseEntities;
using IST.Core.Entities.Dictionaries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Linq.Expressions;

namespace IST.Infrastructure.Data;

public class AppDbContext : DbContextBase, IAppDbContext
{
    //private readonly ICurrentUserService? _currentUser;

    //public AppDbContext(DbContextOptions options, ICurrentUserService? currentUser = null)
    //    : base(options)
    //{
    //    _currentUser = currentUser;
    //}

    public AppDbContext(DbContextOptions options) : base(options) { }

    // === DbSets ===
    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<RoleEntity> Roles { get; set; } = null!;
    public DbSet<UserRolesEntity> UserRoles { get; set; } = null!;

    public DbSet<DictionaryEntity> Dictionaries { get; set; } = null!;
    public DbSet<DictionaryFieldEntity> DictionaryFields { get; set; } = null!;
    public DbSet<DictionaryRecordEntity> DictionaryRecords { get; set; } = null!;

    // Служебные таблицы Fusion для распределённой инвалидации
    public DbSet<DbOperation> Operations { get; protected set; } = null!;
    public DbSet<DbEvent> Events { get; protected set; } = null!;


    public DatabaseFacade Database => base.Database;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<DictionaryRecordEntity>()
            .Property(x => x.Data)
            .HasColumnType("jsonb");

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var filter = Expression.Lambda(
                    Expression.Equal(property, Expression.Constant(false)),
                    parameter
                );
                entityType.SetQueryFilter(filter);
            }
        }

        modelBuilder.DBInitializer();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplySoftDelete();
        ApplyAudit();

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplySoftDelete()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Deleted &&
                entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = DateTime.UtcNow;
                softDeletable.DeletedBy = null; // Здесь можно установить ID текущего пользователя, если он доступен
            }
        }
    }

    /// <summary>
    /// Автоматически заполняет поля аудита CreatedAt/By и UpdatedAt/By
    /// для всех сущностей, реализующих IAuditableEntity.
    /// </summary>
    private void ApplyAudit()
    {
        //var userId = _currentUser?.UserId;
        //var now = DateTime.UtcNow;

        var entries = ChangeTracker.Entries<IAuditableEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = null;
            }

            // UpdatedAt/By ставим и при Added (для единообразия), 
            // и при Modified (в том числе после soft delete)
            entry.Entity.UpdatedAt = DateTime.UtcNow;
            entry.Entity.UpdatedBy = null;
        }
    }
}