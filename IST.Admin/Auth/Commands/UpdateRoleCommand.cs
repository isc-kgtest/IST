using ASIO10.Application.Common.Attributes;
using ASIO10.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ASIO10.Auth.Commands;

using ResponseModel = ASIO10.Domain.EntityModels.Auth.RoleEntity;

[AuthRole("admin")]
public class UpdateRoleCommand : IRequest<ResponseDTO<ResponseModel>>, IAuthorizableRequest
{
    public Ulid Id { get; set; }

    [Required(ErrorMessage = "Название роля обязательно.")]
    [StringLength(50, ErrorMessage = "Название должен содержать от 3 до 50 символов.", MinimumLength = 3)]
    public string Name { get; set; }

    [Required(ErrorMessage = "Описание обязательно.")]
    [StringLength(128, ErrorMessage = "Описание должен содержать от 3 до 128 символов.", MinimumLength = 3)]
    public string Description { get; set; }
    public bool Disabled { get; set; }
    public required UserProfile UserProfile { get; set; }

    public class Handler : IRequestHandler<UpdateRoleCommand, ResponseDTO<ResponseModel>>
    {
        private readonly IAppDbContext _appDbContext;

        public Handler(IAppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ResponseDTO<ResponseModel>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var role = await _appDbContext.Roles
                                    .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

                (string message, ResponseStatusCode statusCode) = true switch
                {
                    _ when role is null
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

                role.Name = request.Name.ToLower().Trim();
                role.Description = request.Description;
                role.Disabled = request.Disabled;

                await _appDbContext.SaveChangesAsync(cancellationToken);

                return new()
                {
                    Status = true,
                    StatusMessage = "Ok",
                    Data = role
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
