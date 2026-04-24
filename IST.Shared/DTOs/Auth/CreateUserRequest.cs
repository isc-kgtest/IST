using MemoryPack;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

[DataContract]
[MemoryPackable]
public partial class CreateUserRequest
{
    [DataMember, MemoryPackOrder(0), Required(ErrorMessage = "Фамилия обязательна")]
    public string Surname { get; set; } = "";

    [DataMember, MemoryPackOrder(1), Required(ErrorMessage = "Имя обязательно")]
    public string Name { get; set; } = "";

    [DataMember, MemoryPackOrder(2)]
    public string? Patronymic { get; set; }

    [DataMember, MemoryPackOrder(3), Required]
    public string Position { get; set; } = "";

    [DataMember, MemoryPackOrder(4)]
    public Guid Organization { get; set; }

    [DataMember, MemoryPackOrder(5), Required]
    public string Department { get; set; } = "";

    [DataMember, MemoryPackOrder(6), Required, EmailAddress]
    public string EMail { get; set; } = "";

    [DataMember, MemoryPackOrder(7), Required]
    public string PhoneNumber { get; set; } = "";

    [DataMember, MemoryPackOrder(8), Required, StringLength(50, MinimumLength = 3)]
    public string Login { get; set; } = "";

    [DataMember, MemoryPackOrder(9), Required]
    public string Password { get; set; } = "";

    [DataMember, MemoryPackOrder(10)]
    public bool IsActive { get; set; } = true;
}
