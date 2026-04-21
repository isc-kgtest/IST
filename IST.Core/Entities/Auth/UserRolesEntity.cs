namespace IST.Core.Entities.Auth;

public class UserRolesEntity : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public UserEntity User { get; set; }
    public RoleEntity Role { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
