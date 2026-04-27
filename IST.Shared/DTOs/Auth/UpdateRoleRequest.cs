using MemoryPack;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

[DataContract]
[MemoryPackable]
public partial class UpdateRoleRequest
{
    [DataMember, MemoryPackOrder(0), Required]
    public Guid Id { get; set; }

    [DataMember, MemoryPackOrder(1)]
    [Required(ErrorMessage = "Название роли обязательно.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Название должно содержать от 3 до 50 символов.")]
    public string Name { get; set; } = "";

    [DataMember, MemoryPackOrder(2)]
    [Required(ErrorMessage = "Описание обязательно.")]
    [StringLength(128, MinimumLength = 3, ErrorMessage = "Описание должно содержать от 3 до 128 символов.")]
    public string Description { get; set; } = "";

    [DataMember, MemoryPackOrder(3)]
    public bool Disabled { get; set; }
}
