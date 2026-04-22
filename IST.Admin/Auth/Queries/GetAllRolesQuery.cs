using IST.Contracts.Features.Auth;

namespace IST.Admin.Auth.Queries;

using ResponseModel = ResponseDTO<List<RoleEntity>>;

public class GetAllRolesQuery : ICommand<ResponseModel>
{
    //public required UserProfile UserProfile { get; set; }

    public class Handler
    {
        private readonly IAuthService _authService;

        public Handler(IAuthService authService)
        {
            _authService = authService;
        }

        [CommandHandler]
        public async Task<ResponseModel> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = await _authService.GetAllRolesAsync(cancellationToken);

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