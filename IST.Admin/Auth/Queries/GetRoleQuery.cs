namespace ASIO10.Auth.Queries;

using ASIO10.Application.Common.Attributes;
using ASIO10.Application.Common.Interfaces;
using ASIO10.Auth.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using ResponseModel = Domain.EntityModels.Auth.RoleEntity;

[AuthRole("admin")]
public class GetRoleQuery : IRequest<ResponseDTO<ResponseModel>>, IAuthorizableRequest
{
    [Required(ErrorMessage = "ID обязателен.")]
    public Ulid Id { get; set; }
    public required UserProfile UserProfile { get; set; }

    public class Handler : IRequestHandler<GetRoleQuery, ResponseDTO<ResponseModel>>
    {
        private readonly IAppDbContext _appDbContext;

        public Handler(IAppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ResponseDTO<ResponseModel>> Handle(GetRoleQuery request, CancellationToken cancellationToken)
        {
            var role = await _appDbContext.Roles.AsNoTracking()
                                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            (string message, ResponseStatusCode statusCode) = true switch
            {
                _ when role is null
                    => ("Не существует", ResponseStatusCode.NotFound),

                // Случай по умолчанию: все проверки пройдены
                _ => ("Валидация пройдена успешно.", ResponseStatusCode.Ok)
            };
           
            if(statusCode == ResponseStatusCode.Ok)
            {
                return new()
                {
                    Data = role,
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