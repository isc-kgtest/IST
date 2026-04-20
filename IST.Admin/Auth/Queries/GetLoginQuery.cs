namespace ASIO10.Auth.Queries;

using ASIO10.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

using ResponseModel = Domain.EntityModels.Auth.UserEntity;

public class GetLoginQuery : IRequest<ResponseDTO<ResponseModel>>
{
    [Required(ErrorMessage = "Логин пользователя обязательно.")]
    public string UserName { get; set; }

    [Required(ErrorMessage = "Пароль обязателен.")]
    public string Password { get; set; }

    public class Handler : IRequestHandler<GetLoginQuery, ResponseDTO<ResponseModel>>
    {
        private readonly IAppDbContext _appDbContext;

        public Handler(IAppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ResponseDTO<ResponseModel>> Handle(GetLoginQuery request, CancellationToken cancellationToken)
        {
            var user = await _appDbContext.Users.AsNoTracking()
                .Include(x => x.UserRoles).ThenInclude(t => t.Role)
                //.Include(x => x.UserRoles).ThenInclude(t => t.Where(x => !x.Role.Disabled).Select(r => r.Role))
                .FirstOrDefaultAsync(x => x.UserName == request.UserName, cancellationToken);

            var passValid = PasswordUtils.VerifyPassword(request.Password, user?.Password ?? null);

            (string message, ResponseStatusCode statusCode) = true switch
            {
                _ when user is null
                    => ("Неверный логин", ResponseStatusCode.NotFound),
                _ when !passValid
                    => ("Неверный пароль", ResponseStatusCode.ValidationError),
                _ when user.Disabled
                    => ("Ваша учетная запись отключена. Пожалуйста, свяжитесь с администратором.", ResponseStatusCode.Unauthorized),
                _ when user.PasswordExpiryDate < DateTime.UtcNow
                    => ("Срок действия пароля пользователя истек.", ResponseStatusCode.PasswordExpired),
                // Случай по умолчанию: все проверки пройдены
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