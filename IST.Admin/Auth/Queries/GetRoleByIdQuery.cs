namespace ASIO10.Auth.Queries;

using IST.Contracts.Features.Auth;
using ResponseModel = ResponseDTO<RoleEntity>;

public class GetRoleByIdQuery : ICommand<ResponseModel>
{
    [Required(ErrorMessage = "ID обязателен.")]
    public Guid Id { get; set; }
    //public required UserProfile UserProfile { get; set; }

    public class Handler
    {
        private readonly IAuthService _authService;

        public Handler(IAuthService authService)
        {
            _authService = authService;
        }

        [CommandHandler]
        public async Task<ResponseModel> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
        {
            var role = await _authService.GetRoleByIdAsync(request.Id, cancellationToken);

            (string message, ResponseStatusCode statusCode) = true switch
            {
                _ when role is null
                    => ("Не существует", ResponseStatusCode.NotFound),

                // все проверки пройдены
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