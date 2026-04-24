using MemoryPack;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

[DataContract]
[MemoryPackable]
public partial class ChangeUserPasswordRequest
{
    [DataMember, MemoryPackOrder(0)]
    [Required(ErrorMessage = "Логин пользователя обязательно.")]
    [StringLength(50, ErrorMessage = "Логин должен содержать от 3 до 50 символов.", MinimumLength = 3)]
    public string Login { get; set; } = "";

    [DataMember, MemoryPackOrder(1)]
    [Required(ErrorMessage = "Текущий пароль обязателен.")]
    [StringLength(128, ErrorMessage = "Пароль должен содержать от 8 до 128 символов.", MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()])[A-Za-z\d!@#$%^&*()]{8,}$",
    ErrorMessage = "Пароль должен содержать минимум 8 символов, включая заглавные и строчные буквы, цифры и один из спецсимволов: !@#$%^&*() ")]
    public string CurrentPassword { get; set; } = "";

    [DataMember, MemoryPackOrder(2)]
    [Required(ErrorMessage = "Пароль обязателен.")]
    [StringLength(128, ErrorMessage = "Пароль должен содержать от 8 до 128 символов.", MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()])[A-Za-z\d!@#$%^&*()]{8,}$",
    ErrorMessage = "Пароль должен содержать минимум 8 символов, включая заглавные и строчные буквы, цифры и один из спецсимволов: !@#$%^&*() ")]
    public string NewPassword { get; set; } = "";

    [DataMember, MemoryPackOrder(3)]
    [Required(ErrorMessage = "Подтверждение пароля обязательно.")]
    [Compare("NewPassword", ErrorMessage = "Пароли не совпадают.")]
    public string ConfirmPassword { get; set; } = "";
}
