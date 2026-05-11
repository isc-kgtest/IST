using MemoryPack;
using IST.Core.Entities.BaseEntities;

namespace IST.Core.Entities.Auth;

public partial class PermissionEntity : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }

    public ICollection<RolePermissionEntity> RolePermissions { get; set; } = new List<RolePermissionEntity>();
}
