using MemoryPack;
using IST.Core.Entities.BaseEntities;

namespace IST.Core.Entities.Auth;

[MemoryPackable]
public partial class UserRolesEntity : BaseEntity
{
    [MemoryPackOrder(8)]
    public Guid UserId { get; set; }
    [MemoryPackOrder(9)]
    public Guid RoleId { get; set; }
    [MemoryPackIgnore]
    public UserEntity User { get; set; }
    [MemoryPackIgnore]
    public RoleEntity Role { get; set; }
    [MemoryPackOrder(10)]
    public DateTime StartDate { get; set; }
    [MemoryPackOrder(11)]
    public DateTime? EndDate { get; set; }
}
