namespace ASIO10.Auth.Queries;

using ASIO10.Application.Common.Attributes;
using ASIO10.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using ResponseModel = List<Domain.EntityModels.Auth.UserEntity>;

[AuthRole("admin")]
public class GetAllUsersQuery : IRequest<ResponseDTO<ResponseModel>>, IAuthorizableRequest
{
    public required UserProfile UserProfile { get; set; }
    public class Handler : IRequestHandler<GetAllUsersQuery, ResponseDTO<ResponseModel>>
    {
        private readonly IAppDbContext _appDbContext;

        public Handler(IAppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ResponseDTO<ResponseModel>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _appDbContext.Users.AsNoTracking().ToListAsync();

            return users is not null ?
            new()
            {
                Status = true,
                StatusMessage = "Ok",
                Data = users
            }
            :
            new()
            {
                Status = false,
                StatusMessage = "No user"
            };
        }
    }
}