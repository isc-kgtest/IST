namespace IST.Core.Entities.Auth;

public class UserEntity : BaseEntity
{
    // ФИО
    public string Surname { get; set; }
    public string Name { get; set; }
    public string? Patronymic { get; set; }

    /// <summary>Вычисляемое полное имя. Не хранится в БД.</summary>
    public string FullName => string.Join(" ",
          new[] { Surname, Name, Patronymic }
              .Where(s => !string.IsNullOrWhiteSpace(s)));

    // Организационные данные
    public string Position { get; set; }
    public string Department { get; set; }

    // Связь с организацией (пока как Guid, позже → навигация)
    public Guid OrganizationId { get; set; }
    // public OrganizationEntity? Organization { get; set; }  — раскомментировать, когда появится справочник

    // Контакты
    public string EMail { get; set; }
    public string PhoneNumber { get; set; }
  

    // Учётные данные
    public string Login { get; set; }
    public string Password { get; set; }
    public DateTime PasswordExpiryDate { get; set; }
    public DateTime? LastDateLogin { get; set; }
    public bool IsActive { get; set; }

    // Электронная подпись (ОЭЦП)
    public string? CertificateThumbprint { get; set; }  // отпечаток сертификата
    public DateTime? CertificateValidUntil { get; set; } // до какой даты действителен

    // Связи
    public ICollection<UserRolesEntity> UserRoles { get; set; } = new List<UserRolesEntity>();
}
