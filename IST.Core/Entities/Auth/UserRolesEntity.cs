using MemoryPack;
using IST.Core.Entities.BaseEntities;

namespace IST.Core.Entities.Auth;

public partial class UserRolesEntity : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public UserEntity User { get; set; } = null!;
    public RoleEntity Role { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
