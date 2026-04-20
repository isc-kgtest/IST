using ASIO10.Application.Common.Attributes;
using ASIO10.Application.Common.Interfaces;
using ASIO10.Auth.Models;
using ASIO10.Domain.EntityModels.Auth;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ASIO10.Auth.Queries;

using ResponseModel = ASIO10.Domain.EntityModels.Auth.UserRolesEntity;

[AuthRole("admin")]
public class UpdateUserRoleCommand : IRequest<ResponseDTO<ResponseModel>>, IAuthorizableRequest
{
    [Required(ErrorMessage = "Id обязательно.")]
    public Ulid UserId { get; set; }

    [Required(ErrorMessage = "Id обязательно.")]
    public Ulid RoleId { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public required UserProfile UserProfile { get; set; }
    public class Handler : IRequestHandler<UpdateUserRoleCommand, ResponseDTO<ResponseModel>>
    {
        private readonly IAppDbContext _appDbContext;

        public Handler(IAppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ResponseDTO<ResponseModel>> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userRole = await _appDbContext.UserRoles.FirstOrDefaultAsync(x => x.RoleId == request.RoleId && x.UserId == request.UserId, cancellationToken);

                (string message, ResponseStatusCode statusCode) = true switch
                {
                    _ when userRole is null
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

                _appDbContext.UserRoles.Update(userRole);

                await _appDbContext.SaveChangesAsync(cancellationToken);

                return new()
                {
                    Status = true,
                    StatusMessage = "Ok",
                    Data = userRole
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