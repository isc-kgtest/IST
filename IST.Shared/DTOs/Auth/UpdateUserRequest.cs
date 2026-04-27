using MemoryPack;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

[DataContract]
[MemoryPackable]
public partial class UpdateUserRequest
{
    [DataMember, MemoryPackOrder(0), Required]
    public Guid Id { get; set; }

    [DataMember, MemoryPackOrder(1), Required(ErrorMessage = "Фамилия обязательна")]
    public string Surname { get; set; } = "";

    [DataMember, MemoryPackOrder(2), Required(ErrorMessage = "Имя обязательно")]
    public string Name { get; set; } = "";

    [DataMember, MemoryPackOrder(3)]
    public string? Patronymic { get; set; }

    [DataMember, MemoryPackOrder(4), Required]
    public string Position { get; set; } = "";

    [DataMember, MemoryPackOrder(5)]
    public Guid Organization { get; set; }

    [DataMember, MemoryPackOrder(6)]
    public string? Department { get; set; }

    [DataMember, MemoryPackOrder(7), Required, EmailAddress]
    public string EMail { get; set; } = "";

    [DataMember, MemoryPackOrder(8), Required]
    public string PhoneNumber { get; set; } = "";

    [DataMember, MemoryPackOrder(9)]
    public bool IsActive { get; set; }
}
