using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

[DataContract]
public class CreateRoleRequest
{
    [DataMember]
    [Required(ErrorMessage = "Название роли обязательно.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Название должно содержать от 3 до 50 символов.")]
    public string Name { get; set; } = "";

    [DataMember]
    [Required(ErrorMessage = "Описание обязательно.")]
    [StringLength(128, MinimumLength = 3, ErrorMessage = "Описание должно содержать от 3 до 128 символов.")]
    public string Description { get; set; } = "";
}