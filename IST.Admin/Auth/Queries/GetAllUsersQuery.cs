using IST.Contracts.Features.Auth;

namespace IST.Admin.Auth.Queries;

using ResponseModel = ResponseDTO<List<UserEntity>>;

public class GetAllUsersQuery : ICommand<ResponseModel>
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
        public async Task<ResponseModel> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _authService.GetAllUsersAsync(cancellationToken);

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