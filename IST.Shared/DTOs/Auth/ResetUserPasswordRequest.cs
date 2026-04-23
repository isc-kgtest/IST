using System.ComponentModel.DataAnnotations;

namespace IST.Shared.DTOs.Auth;

public class ResetUserPasswordRequest
{
    [Required(ErrorMessage = "Логин пользователя обязательно.")]
    [StringLength(50, ErrorMessage = "Логин должен содержать от 3 до 50 символов.", MinimumLength = 3)]
    public string Login { get; set; }

    [Required(ErrorMessage = "Пароль обязателен.")]
    [StringLength(128, ErrorMessage = "Пароль должен содержать от 8 до 128 символов.", MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()])[A-Za-z\d!@#$%^&*()]{8,}$",
    ErrorMessage = "Пароль должен содержать минимум 8 символов, включая заглавные и строчные буквы, цифры и один из спецсимволов: !@#$%^&*() ")]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "Подтверждение пароля обязательно.")] // ConfirmPassword тоже должно быть обязательным
    [Compare("NewPassword", ErrorMessage = "Пароли не совпадают.")] // Сравниваем с NewPassword
    public string ConfirmPassword { get; set; }

    public bool ResetPassword { get; set; }
}
