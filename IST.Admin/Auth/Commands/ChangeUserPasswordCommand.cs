using ASIO10.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ASIO10.Auth.Queries;

using ResponseModel = string;

public class ChangeUserPasswordCommand : IRequest<ResponseDTO<ResponseModel>>
{
    [Required(ErrorMessage = "Логин пользователя обязательно.")]
    [StringLength(50, ErrorMessage = "Логин должен содержать от 3 до 50 символов.", MinimumLength = 3)]
    public string UserName { get; set; }

    [Required(ErrorMessage = "Текущий пароль обязателен.")]
    [StringLength(128, ErrorMessage = "Пароль должен содержать от 8 до 128 символов.", MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()])[A-Za-z\d!@#$%^&*()]{8,}$",
    ErrorMessage = "Пароль должен содержать минимум 8 символов, включая заглавные и строчные буквы, цифры и один из спецсимволов: !@#$%^&*() ")]
    public string CurrentPassword { get; set; }

    [Required(ErrorMessage = "Пароль обязателен.")]
    [StringLength(128, ErrorMessage = "Пароль должен содержать от 8 до 128 символов.", MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()])[A-Za-z\d!@#$%^&*()]{8,}$",
    ErrorMessage = "Пароль должен содержать минимум 8 символов, включая заглавные и строчные буквы, цифры и один из спецсимволов: !@#$%^&*() ")]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "Подтверждение пароля обязательно.")] // ConfirmPassword тоже должно быть обязательным
    [Compare("NewPassword", ErrorMessage = "Пароли не совпадают.")] // Сравниваем с NewPassword
    public string ConfirmPassword { get; set; }

    public class Handler : IRequestHandler<ChangeUserPasswordCommand, ResponseDTO<ResponseModel>>
    {
        private readonly IAppDbContext _appDbContext;

        public Handler(IAppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ResponseDTO<ResponseModel>> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Сразу деконструируем результат в переменные с понятными именами
                (string message, ResponseStatusCode statusCode) = true switch
                {
                    // Проверка на совпадение со старым паролем
                    _ when request.CurrentPassword == request.NewPassword
                        => ("Новый пароль не должен совпадать с текущим.", ResponseStatusCode.ValidationError),

                    // Проверка на совпадение с подтверждением
                    _ when request.ConfirmPassword != request.NewPassword
                        => ("Новый пароль и его подтверждение не совпадают.", ResponseStatusCode.ValidationError),

                    // Случай по умолчанию: все проверки пройдены
                    _ => ("Валидация пройдена успешно.", ResponseStatusCode.Ok)
                };

                if (statusCode != ResponseStatusCode.Ok)
                {
                    return new()
                    {
                        Status = false,
                        StatusMessage = message,
                        StatusCode = statusCode,
                    };
                }

                var user = await _appDbContext.Users.FirstOrDefaultAsync(x => x.UserName == request.UserName, cancellationToken);

                var passValid = PasswordUtils.VerifyPassword(request.CurrentPassword, user?.Password ?? null);

                (message, statusCode) = true switch
                {
                    _ when user is null
                        => ("Неверный логин", ResponseStatusCode.NotFound),

                    _ when user.Disabled
                        => ("Ваша учетная запись отключена. Пожалуйста, свяжитесь с администратором.", ResponseStatusCode.Unauthorized),

                    _ when !passValid
                        => ("Неверный пароль", ResponseStatusCode.Unauthorized),

                    // Случай по умолчанию: все проверки пройдены
                    _ => ("Валидация пройдена успешно.", ResponseStatusCode.Ok)
                };

                if (statusCode != ResponseStatusCode.Ok)
                {
                    return new()
                    {
                        Status = false,
                        StatusMessage = message,
                        StatusCode = statusCode
                    };
                }
                else
                {
                    user.Password = PasswordUtils.HashPassword(request.NewPassword);
                    user.PasswordExpiryDate = DateTime.UtcNow.AddMonths(3);

                    _appDbContext.Users.Update(user);

                    await _appDbContext.SaveChangesAsync(cancellationToken);

                    return new()
                    {
                        Data = user.UserName,
                        Status = true,
                        StatusMessage = "Пароль успешно изменен",
                        StatusCode = ResponseStatusCode.Ok
                    };
                }
            }
            catch (Exception ex)
            {
                return new()
                {
                    Status = false,
                    StatusMessage = ex.Message,
                    StatusCode = ResponseStatusCode.InternalServerError
                };
            }
        }
    }
}