using ASIO10.Application.Common.Attributes;
using ASIO10.Application.Common.Interfaces;
using ASIO10.Auth.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ASIO10.Auth.Queries;

using ResponseModel = string;

[AuthRole("admin")]
public class DeleteRoleCommand : IRequest<ResponseDTO<ResponseModel>>, IAuthorizableRequest
{
    [Required(ErrorMessage = "Id обязательно.")]
    public Ulid Id { get; set; }

    public required UserProfile UserProfile { get; set; }

    public class Handler : IRequestHandler<DeleteRoleCommand, ResponseDTO<ResponseModel>>
    {
        private readonly IAppDbContext _appDbContext;

        public Handler(IAppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ResponseDTO<ResponseModel>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var roleToDelete = await _appDbContext.Roles.Include(r => r.UserRoles).FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

                // Выполняем проверку и сразу получаем сообщение и код статуса
                (string message, ResponseStatusCode statusCode) = (true) switch
                {
                    _ when roleToDelete is null
                       => ("Роль не найден", ResponseStatusCode.NotFound),

                    // В остальных случаях считаем, что валидация прошла успешно
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

                roleToDelete.IsDeleted = true;

                foreach (var userRole in roleToDelete.UserRoles)
                {
                    userRole.IsDeleted = true;
                }

                await _appDbContext.SaveChangesAsync(cancellationToken);

                return new()
                {
                    Status = true,
                    StatusMessage = "Ok",
                    Data = roleToDelete.Name
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