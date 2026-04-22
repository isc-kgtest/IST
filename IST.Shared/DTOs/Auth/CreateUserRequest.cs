using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

[DataContract] // Обязательно для сериализации во Fusion
public class CreateUserRequest
{
    [DataMember, Required(ErrorMessage = "Фамилия обязательна")]
    public string Surname { get; set; } = "";

    [DataMember, Required(ErrorMessage = "Имя обязательно")]
    public string Name { get; set; } = "";

    [DataMember]
    public string? Patronymic { get; set; }

    [DataMember, Required]
    public string Position { get; set; } = "";

    [DataMember]
    public Guid Organization { get; set; }

    [DataMember, Required]
    public string Department { get; set; } = "";

    [DataMember, Required, EmailAddress]
    public string EMail { get; set; } = "";

    [DataMember, Required]
    public string PhoneNumber { get; set; } = "";

    [DataMember, Required, StringLength(50, MinimumLength = 3)]
    public string Login { get; set; } = "";

    [DataMember, Required]
    public string Password { get; set; } = "";

    [DataMember]
    public bool IsActive { get; set; } = true;
}