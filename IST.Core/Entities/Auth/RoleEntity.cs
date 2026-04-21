namespace IST.Core.Entities.Auth;

public class RoleEntity : BaseEntity
{
    public string Name { get; set; }
    public string? Description { get; set; }

    public ICollection<UserRolesEntity> UserRoles { get; set; } = new List<UserRolesEntity>();
}
