using MemoryPack;
using IST.Core.Entities.BaseEntities;

namespace IST.Core.Entities.Auth;

[MemoryPackable]
public partial class UserEntity : BaseEntity
{
    // ФИО
    [MemoryPackOrder(8)]
    public string Surname { get; set; }
    [MemoryPackOrder(9)]
    public string Name { get; set; }
    [MemoryPackOrder(10)]
    public string? Patronymic { get; set; }

    /// <summary>Вычисляемое полное имя. Не хранится в БД.</summary>
    [MemoryPackIgnore]
    public string FullName => string.Join(" ",
          new[] { Surname, Name, Patronymic }
              .Where(s => !string.IsNullOrWhiteSpace(s)));

    // Организационные данные
    [MemoryPackOrder(11)]
    public string Position { get; set; }
    [MemoryPackOrder(12)]
    public string Department { get; set; }

    // Связь с организацией (пока как Guid, позже → навигация)
    [MemoryPackOrder(13)]
    public Guid OrganizationId { get; set; }
    // public OrganizationEntity? Organization { get; set; }  — раскомментировать, когда появится справочник

    // Контакты
    [MemoryPackOrder(14)]
    public string EMail { get; set; }
    [MemoryPackOrder(15)]
    public string PhoneNumber { get; set; }
  

    // Учётные данные
    [MemoryPackOrder(16)]
    public string Login { get; set; }
    [MemoryPackOrder(17)]
    public string Password { get; set; }
    [MemoryPackOrder(18)]
    public DateTime PasswordExpiryDate { get; set; }
    [MemoryPackOrder(19)]
    public DateTime? LastDateLogin { get; set; }
    [MemoryPackOrder(20)]
    public bool IsActive { get; set; }

    // Электронная подпись (ОЭЦП)
    [MemoryPackOrder(21)]
    public string? CertificateThumbprint { get; set; }  // отпечаток сертификата
    [MemoryPackOrder(22)]
    public DateTime? CertificateValidUntil { get; set; } // до какой даты действителен

    // Связи
    [MemoryPackIgnore] // Обычно навигационные свойства не сериализуют в RPC чтобы избежать циклов
    public ICollection<UserRolesEntity> UserRoles { get; set; } = new List<UserRolesEntity>();
}
