namespace IST.Core.Entities.Auth;

public class UserEntity : BaseEntity
{
    public string Surname { get; set; }
    public string Name { get; set; }
    public string? Patronymic { get; set; }
    public string FullName => $"{Surname} {Name} {Patronymic}".Trim();
    public string Position { get; set; }
    public Guid Organization { get; set; }
    public string Department { get; set; }
    public string EMail { get; set; }
    public string PhoneNumber { get; set; }
    public ICollection<UserRolesEntity> UserRoles { get; set; } = new List<UserRolesEntity>();
    public string Login { get; set; }
    public string Password { get; set; }
    public DateTime LastDateLogin { get; set; }
    public bool IsActive { get; set; }
    public string? DigitalSignature { get; set; }
    public DateTime PasswordExpiryDate { get; set; }

   
}
