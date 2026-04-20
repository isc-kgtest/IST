namespace ASIO10.Auth.Queries;

using ASIO10.Application.Common.Attributes;
using ASIO10.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using ResponseModel = List<Domain.EntityModels.Auth.RoleEntity>;

[AuthRole("admin")]
public class GetAllRolesQuery : IRequest<ResponseDTO<ResponseModel>>, IAuthorizableRequest
{
    public required UserProfile UserProfile { get; set; }
    public class Handler : IRequestHandler<GetAllRolesQuery, ResponseDTO<ResponseModel>>
    {
        private readonly IAppDbContext _appDbContext;

        public Handler(IAppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ResponseDTO<ResponseModel>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = await _appDbContext.Roles.AsNoTracking().ToListAsync();

            return roles is not null ?
            new()
            {
                Status = true,
                StatusMessage = "Ok",
                Data = roles
            }
            :
            new()
            {
                Status = false,
                StatusMessage = "No roles"
            };
        }
    }
}