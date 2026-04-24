using MemoryPack;
using IST.Core.Entities.BaseEntities;

namespace IST.Core.Entities.Auth;

[MemoryPackable]
public partial class RoleEntity : BaseEntity
{
    [MemoryPackOrder(8)]
    public string Name { get; set; }
    [MemoryPackOrder(9)]
    public string? Description { get; set; }

    [MemoryPackIgnore]
    public ICollection<UserRolesEntity> UserRoles { get; set; } = new List<UserRolesEntity>();
}
