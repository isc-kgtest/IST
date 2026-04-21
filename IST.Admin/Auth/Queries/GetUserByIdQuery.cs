namespace IST.Admin.Auth.Queries;

using IST.Services.Features.Auth;
using ResponseModel = ResponseDTO<UserEntity>;

public class GetUserByIdQuery : ICommand<ResponseModel>
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
        public async Task<ResponseModel> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _authService.GetUserByIdAsync(request.Id, cancellationToken);
            (string message, ResponseStatusCode statusCode) = true switch
            {
                _ when user is null
                    => ("Не существует", ResponseStatusCode.NotFound),

                // все проверки пройдены
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