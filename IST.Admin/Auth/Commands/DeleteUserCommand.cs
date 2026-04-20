using ASIO10.Application.Common.Attributes;
using ASIO10.Application.Common.Interfaces;
using ASIO10.Auth.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace ASIO10.Auth.Queries;

using ResponseModel = string;

[AuthRole("admin")]
public class DeleteUserCommand : IRequest<ResponseDTO<ResponseModel>>, IAuthorizableRequest
{
    [Required(ErrorMessage = "Id обязательно.")]
    public Ulid Id { get; set; }

    public required UserProfile UserProfile { get; set; }

    public class Handler : IRequestHandler<DeleteUserCommand, ResponseDTO<ResponseModel>>
    {
        private readonly IAppDbContext _appDbContext;

        public Handler(IAppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ResponseDTO<ResponseModel>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userToDelete = await _appDbContext.Users.Include(x => x.UserRoles)
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                // Выполняем проверку и сразу получаем сообщение и код статуса
                (string message, ResponseStatusCode statusCode) = (true) switch
                {
                    _ when userToDelete is null
                       => ("Ресурс не найден", ResponseStatusCode.NotFound),

                    // Проверка: попытка удалить самого себя
                    _ when request?.UserProfile.UserSession.UserId == request.Id
                        => ("Нельзя удалить собственную учётную запись.", ResponseStatusCode.ValidationError),

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

                userToDelete.IsDeleted = true;

                foreach(var userRole in userToDelete.UserRoles)
                {
                    userRole.IsDeleted = true;
                }

                await _appDbContext.SaveChangesAsync(cancellationToken);
                
                return new()
                {
                    Status = true,
                    StatusMessage = "Ok",
                    Data = userToDelete.UserName
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