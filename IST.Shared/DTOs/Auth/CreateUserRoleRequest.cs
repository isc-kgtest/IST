using MemoryPack;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

[DataContract]
[MemoryPackable]
public partial class CreateUserRoleRequest
{
    [DataMember, MemoryPackOrder(0), Required]
    public Guid UserId { get; set; }

    [DataMember, MemoryPackOrder(1), Required]
    public Guid RoleId { get; set; }

    /// <summary>Дата начала действия роли. Если не указана — текущий момент.</summary>
    [DataMember, MemoryPackOrder(2)]
    public DateTime? StartDate { get; set; }

    /// <summary>Дата окончания. null = постоянная роль.</summary>
    [DataMember, MemoryPackOrder(3)]
    public DateTime? EndDate { get; set; }
}
