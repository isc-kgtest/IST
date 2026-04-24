using MemoryPack;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

[DataContract]
[MemoryPackable]
public partial class UpdateUserRoleRequest
{
    [DataMember, MemoryPackOrder(0), Required]
    public Guid Id { get; set; }

    [DataMember, MemoryPackOrder(1), Required]
    public Guid UserId { get; set; }

    [DataMember, MemoryPackOrder(2), Required]
    public DateTime StartDate { get; set; }

    [DataMember, MemoryPackOrder(3)]
    public DateTime? EndDate { get; set; }
}
