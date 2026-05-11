using ActualLab.CommandR.Configuration;
using ActualLab.Fusion.Authentication;
using IST.Contracts.Features.Organization;
using IST.Contracts.Features.Organization.Commands;
using IST.Core.Entities.Auth;
using IST.Core.Entities.BaseEntities;
using IST.Core.Entities.Organization;
using IST.Services.Features.Auth.Authentication;
using IST.Shared.DTOs.Organization;

namespace IST.Services.Features.Organization;

public class OrganizationCommands : IOrganizationCommands
{
    private readonly DbHub<AppDbContext> _dbHub;
    private readonly IOrganizationQueries _queries;
    private readonly ICurrentUserStore _users;
    private readonly IAuth _auth;

    public OrganizationCommands(
        DbHub<AppDbContext> dbHub,
        IOrganizationQueries queries,
        ICurrentUserStore users,
        IAuth auth)
    {
        _dbHub = dbHub;
        _queries = queries;
        _users = users;
        _auth = auth;
    }

    private async ValueTask<IDisposable> BeginAuditScopeAsync(Session session, CancellationToken ct)
    {
        var caller = await _users.TryFindCallerAsync(_auth, session, ct);
        return AuditContext.Begin(caller?.UserId);
    }

    // ==================== NodeTypes ====================

    [CommandHandler]
    public virtual async Task<ResponseDTO<OrganizationNodeTypeDto>> SaveNodeTypeAsync(
        SaveNodeTypeCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetNodeTypesAsync(default);
            return default!;
        }

        await _users.RequirePermissionAsync(_auth, command.Session, cancellationToken, Permissions.OrganizationManage);

        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var db = await _dbHub.CreateOperationDbContext(cancellationToken);

        OrganizationNodeTypeEntity entity;
        var normalizedCode = command.Code.Trim().ToLowerInvariant();

        if (command.Id.HasValue && command.Id.Value != Guid.Empty)
        {
            entity = await db.OrganizationNodeTypes.FirstOrDefaultAsync(t => t.Id == command.Id.Value, cancellationToken)
                     ?? throw new InvalidOperationException("Тип узла не найден.");
        }
        else
        {
            entity = new OrganizationNodeTypeEntity();
            db.OrganizationNodeTypes.Add(entity);
        }

        var codeConflict = await db.OrganizationNodeTypes
            .AnyAsync(t => t.Code == normalizedCode && t.Id != entity.Id, cancellationToken);
        if (codeConflict)
        {
            return new ResponseDTO<OrganizationNodeTypeDto>
            {
                Status = false,
                StatusMessage = "Тип узла с таким кодом уже существует.",
                StatusCode = ResponseStatusCode.ValidationError,
            };
        }

        entity.Code = normalizedCode;
        entity.Name = command.Name.Trim();
        entity.Description = command.Description?.Trim();
        entity.Level = command.Level;
        entity.SortOrder = command.SortOrder;
        entity.Icon = command.Icon?.Trim();

        await db.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<OrganizationNodeTypeDto>
        {
            Status = true,
            StatusMessage = "Сохранено.",
            StatusCode = ResponseStatusCode.Ok,
            Data = new OrganizationNodeTypeDto
            {
                Id = entity.Id, Code = entity.Code, Name = entity.Name, Description = entity.Description,
                Level = entity.Level, SortOrder = entity.SortOrder, Icon = entity.Icon,
            },
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteNodeTypeAsync(
        DeleteNodeTypeCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetNodeTypesAsync(default);
            return default!;
        }

        await _users.RequirePermissionAsync(_auth, command.Session, cancellationToken, Permissions.OrganizationManage);

        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var db = await _dbHub.CreateOperationDbContext(cancellationToken);

        var inUse = await db.OrganizationNodes.AnyAsync(n => n.NodeTypeId == command.Id, cancellationToken);
        if (inUse)
        {
            return new ResponseDTO<string>
            {
                Status = false,
                StatusMessage = "Тип узла используется существующими узлами; сначала перенесите или удалите их.",
                StatusCode = ResponseStatusCode.ValidationError,
            };
        }

        var entity = await db.OrganizationNodeTypes.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
        if (entity is null)
            return new ResponseDTO<string> { Status = false, StatusMessage = "Не найдено.", StatusCode = ResponseStatusCode.NotFound };

        db.OrganizationNodeTypes.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<string> { Status = true, Data = entity.Name, StatusCode = ResponseStatusCode.Ok };
    }

    // ==================== Nodes ====================

    [CommandHandler]
    public virtual async Task<ResponseDTO<OrganizationNodeDto>> SaveNodeAsync(
        SaveNodeCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetChildrenAsync(command.ParentNodeId, default);
            _ = _queries.GetAllNodesFlatAsync(default);
            if (command.Id.HasValue) _ = _queries.GetNodeAsync(command.Id.Value, default);
            return default!;
        }

        await _users.RequirePermissionAsync(_auth, command.Session, cancellationToken, Permissions.OrganizationManage);

        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var db = await _dbHub.CreateOperationDbContext(cancellationToken);

        OrganizationNodeEntity entity;
        if (command.Id.HasValue && command.Id.Value != Guid.Empty)
        {
            entity = await db.OrganizationNodes.FirstOrDefaultAsync(n => n.Id == command.Id.Value, cancellationToken)
                     ?? throw new InvalidOperationException("Узел не найден.");
        }
        else
        {
            entity = new OrganizationNodeEntity { Id = Guid.NewGuid() };
            db.OrganizationNodes.Add(entity);
        }

        // Защита от циклов: запрещаем переносить узел под собственного потомка.
        if (command.ParentNodeId.HasValue && command.Id.HasValue && command.ParentNodeId.Value != Guid.Empty)
        {
            var parent = await db.OrganizationNodes.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == command.ParentNodeId.Value, cancellationToken);
            if (parent is null)
            {
                return new ResponseDTO<OrganizationNodeDto>
                {
                    Status = false,
                    StatusMessage = "Указанный родитель не найден.",
                    StatusCode = ResponseStatusCode.ValidationError,
                };
            }
            if (parent.Path.Contains("/" + entity.Id + "/", StringComparison.Ordinal)
                || parent.Id == entity.Id)
            {
                return new ResponseDTO<OrganizationNodeDto>
                {
                    Status = false,
                    StatusMessage = "Нельзя сделать узел потомком самого себя.",
                    StatusCode = ResponseStatusCode.ValidationError,
                };
            }
        }

        entity.NodeTypeId = command.NodeTypeId;
        entity.ParentNodeId = command.ParentNodeId;
        entity.Name = command.Name.Trim();
        entity.Code = command.Code?.Trim();
        entity.Description = command.Description?.Trim();
        entity.SortOrder = command.SortOrder;
        entity.IsActive = command.IsActive;

        // Пересчёт Path и Depth.
        if (command.ParentNodeId.HasValue && command.ParentNodeId.Value != Guid.Empty)
        {
            var parent = await db.OrganizationNodes.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == command.ParentNodeId.Value, cancellationToken);
            entity.Path = (parent?.Path ?? "/") + entity.Id + "/";
            entity.Depth = (parent?.Depth ?? -1) + 1;
        }
        else
        {
            entity.Path = "/" + entity.Id + "/";
            entity.Depth = 0;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<OrganizationNodeDto>
        {
            Status = true,
            Data = new OrganizationNodeDto
            {
                Id = entity.Id,
                NodeTypeId = entity.NodeTypeId,
                ParentNodeId = entity.ParentNodeId,
                Name = entity.Name, Code = entity.Code,
                Path = entity.Path, Depth = entity.Depth,
                SortOrder = entity.SortOrder, IsActive = entity.IsActive,
                Description = entity.Description,
            },
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteNodeAsync(
        DeleteNodeCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetChildrenAsync(null, default);
            _ = _queries.GetNodeAsync(command.Id, default);
            _ = _queries.GetAllNodesFlatAsync(default);
            return default!;
        }

        await _users.RequirePermissionAsync(_auth, command.Session, cancellationToken, Permissions.OrganizationManage);

        using var __auditScope = await BeginAuditScopeAsync(command.Session, cancellationToken);
        await using var db = await _dbHub.CreateOperationDbContext(cancellationToken);

        var entity = await db.OrganizationNodes.Include(n => n.Children)
            .FirstOrDefaultAsync(n => n.Id == command.Id, cancellationToken);
        if (entity is null)
            return new ResponseDTO<string> { Status = false, StatusMessage = "Не найдено.", StatusCode = ResponseStatusCode.NotFound };

        if (entity.Children.Count > 0)
        {
            return new ResponseDTO<string>
            {
                Status = false,
                StatusMessage = "У узла есть подузлы; сначала удалите или перенесите их.",
                StatusCode = ResponseStatusCode.ValidationError,
            };
        }

        db.OrganizationNodes.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);

        // Дополнительная Fusion-инвалидация для устаревшего списка детей.
        using (Invalidation.Begin())
            _ = _queries.GetChildrenAsync(entity.ParentNodeId, default);

        return new ResponseDTO<string> { Status = true, Data = entity.Name, StatusCode = ResponseStatusCode.Ok };
    }
}
