using ASIO10.Application.Common.Attributes;
using ASIO10.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ASIO10.Auth.Queries;

using ResponseModel = ASIO10.Domain.EntityModels.Auth.UserEntity;

[AuthRole("admin")]
public class UpdateUserCommand : IRequest<ResponseDTO<ResponseModel>>, IAuthorizableRequest
{
    public Ulid Id { get; set; }

    [Required(ErrorMessage = "Логин пользователя обязательно.")]
    [StringLength(50, ErrorMessage = "Логин должен содержать от 3 до 50 символов.", MinimumLength = 3)]
    public string UserName { get; set; }

    [Required(ErrorMessage = "Полное имя обязательно.")]
    [StringLength(128, ErrorMessage = "Полное имя должен содержать от 8 до 128 символов.", MinimumLength = 8)]
    public string FullName { get; set; }
    public DateTime? PasswordExpiryDate { get; set; }
    public bool Disabled { get; set; }
    public required UserProfile UserProfile { get; set; }

    public class Handler : IRequestHandler<UpdateUserCommand, ResponseDTO<ResponseModel>>
    {
        private readonly IAppDbContext _appDbContext;

        public Handler(IAppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ResponseDTO<ResponseModel>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _appDbContext.Users
                                    .Include(x => x.UserRoles).ThenInclude(t => t.Role)
                                    .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

                (string message, ResponseStatusCode statusCode) = true switch
                {
                    _ when user is null
                        => ("Не существует", ResponseStatusCode.NotFound),

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

                user.UserName = request.UserName.ToLower().Trim();
                user.FullName = request.FullName;
                user.PasswordExpiryDate = request.PasswordExpiryDate ?? user.PasswordExpiryDate;
                user.Disabled = request.Disabled;

                await _appDbContext.SaveChangesAsync(cancellationToken);

                return new()
                {
                    Status = true,
                    StatusMessage = "Ok",
                    Data = user
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    Status = false,
                    StatusMessage = ex.Message,
                };
            }
        }
    }
}