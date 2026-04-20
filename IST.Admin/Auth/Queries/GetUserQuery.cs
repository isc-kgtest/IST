namespace ASIO10.Auth.Queries;

using ASIO10.Application.Common.Attributes;
using ASIO10.Application.Common.Interfaces;
using ASIO10.Auth.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using ResponseModel = Domain.EntityModels.Auth.UserEntity;

[AuthRole("admin")]
public class GetUserQuery : IRequest<ResponseDTO<ResponseModel>>, IAuthorizableRequest
{
    [Required(ErrorMessage = "ID обязателен.")]
    public Ulid Id { get; set; }

    public required UserProfile UserProfile { get; set; }
    public class Handler : IRequestHandler<GetUserQuery, ResponseDTO<ResponseModel>>
    {
        private readonly IAppDbContext _appDbContext;

        public Handler(IAppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ResponseDTO<ResponseModel>> Handle(GetUserQuery request, CancellationToken cancellationToken)
        {
            var user = await _appDbContext.Users.AsNoTracking()
                                .Include(x => x.UserRoles).ThenInclude(t => t.Role)
                                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            (string message, ResponseStatusCode statusCode) = true switch
            {
                _ when user is null
                    => ("Не существует", ResponseStatusCode.NotFound),

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