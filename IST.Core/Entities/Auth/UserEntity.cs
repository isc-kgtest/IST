using MemoryPack;
using IST.Core.Entities.BaseEntities;

namespace IST.Core.Entities.Auth;

public partial class UserEntity : BaseEntity
{

    // ФИО
    public string Surname { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Patronymic { get; set; }

    /// <summary>Вычисляемое полное имя. Не хранится в БД.</summary>
    public string FullName => string.Join(" ",
          new[] { Surname, Name, Patronymic }
              .Where(s => !string.IsNullOrWhiteSpace(s)));

    // Организационные данные
    public string Position { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    // Связь с организацией (пока как Guid, позже → навигация)
    public Guid OrganizationId { get; set; }

    // Контакты
    public string EMail { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    // Учётные данные
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime PasswordExpiryDate { get; set; }
    public DateTime? LastDateLogin { get; set; }
    public bool IsActive { get; set; }

    // Электронная подпись (ОЭЦП)
    public string? CertificateThumbprint { get; set; }  // отпечаток сертификата
    public DateTime? CertificateValidUntil { get; set; } // до какой даты действителен

    public ICollection<UserRolesEntity> UserRoles { get; set; } = new List<UserRolesEntity>();
}
