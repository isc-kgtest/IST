using IST.Services.Features.Auth;

namespace IST.Admin.Auth.Queries;

using ResponseModel = ResponseDTO<UserEntity>;

public class GetLoginQuery : ICommand<ResponseModel>
{
    [Required(ErrorMessage = "Логин пользователя обязательно.")]
    public string Login { get; set; }

    [Required(ErrorMessage = "Пароль обязателен.")]
    public string Password { get; set; }

    public class Handler
    {
        private readonly IAuthService _authService;

        public Handler(IAuthService authService)
        {
            _authService = authService;
        }

        [CommandHandler]
        public async Task<ResponseModel> Handle(GetLoginQuery request, CancellationToken cancellationToken)
        {
            var user = await _authService.GetUserByLoginAsync(request.Login, request.Password, cancellationToken);

            if(user is null)
            {
                return new()
                {
                    Status = false,
                    StatusMessage = "Неверный логин",
                    StatusCode = ResponseStatusCode.NotFound
                };
            }

            var passValid = PasswordUtils.VerifyPassword(request.Password, user.Password);

            (string message, ResponseStatusCode statusCode) = true switch
            {
                _ when !passValid
                    => ("Неверный пароль", ResponseStatusCode.ValidationError),
                _ when user.IsActive
                    => ("Ваша учетная запись отключена. Пожалуйста, свяжитесь с администратором.", ResponseStatusCode.Unauthorized),
                _ when user.PasswordExpiryDate < DateTime.UtcNow
                    => ("Срок действия пароля пользователя истек.", ResponseStatusCode.PasswordExpired),
                // все проверки пройдены
                _ => ("Валидация пройдена успешно.", ResponseStatusCode.Ok)
            };
           
            if(statusCode == ResponseStatusCode.Ok)
            {
                return new()
                {
                    Data = user,
                    Status = statusCode == ResponseStatusCode.Ok ? true : false,
                    StatusMessage = message,
                    StatusCode = statusCode
                };
            }
            else
            {
                return new()
                {
                    Status = statusCode == ResponseStatusCode.Ok ? true : false,
                    StatusMessage = message,
                    StatusCode = statusCode
                };
            }
        }
    }
}