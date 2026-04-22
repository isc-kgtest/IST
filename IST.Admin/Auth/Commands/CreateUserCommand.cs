namespace ASIO10.Auth.Queries;

using ResponseModel = string;

[AuthRole("admin")]
public class CreateUserCommand : IRequest<ResponseDTO<ResponseModel>>, IAuthorizableRequest
{
    [Required(ErrorMessage = "Логин пользователя обязательно.")]
    [StringLength(50, ErrorMessage = "Логин должен содержать от 3 до 50 символов.", MinimumLength = 3)]
    public string UserName { get; set; }

    [Required(ErrorMessage = "Полное имя обязательно.")]
    [StringLength(128, ErrorMessage = "Полное имя должен содержать от 8 до 128 символов.", MinimumLength = 8)]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Пароль обязателен.")]
    [StringLength(128, ErrorMessage = "Пароль должен содержать от 8 до 128 символов.", MinimumLength = 8)]
    /*
     * Это регулярное выражение проверяет все условия:
     * (?=.*[a-z]) - должна быть хотя бы одна строчная буква
     * (?=.*[A-Z]) - должна быть хотя бы одна заглавная буква
     * (?=.*\d) - должна быть хотя бы одна цифра
     * (?=.*[!@#$%^&*()]) - должен быть хотя бы один из этих спецсимволов
     */
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()])[A-Za-z\d!@#$%^&*()]{8,}$",
    ErrorMessage = "Пароль должен содержать минимум 8 символов, включая заглавные и строчные буквы, цифры и один из спецсимволов: !@#$%^&*() ")]
    public string Password { get; set; }

    public bool ResetPassword { get; set; }

    public required UserProfile UserProfile { get; set; }

    public class Handler : IRequestHandler<CreateUserCommand, ResponseDTO<ResponseModel>>
    {
        private readonly IAppDbContext _appDbContext;

        public Handler(IAppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ResponseDTO<ResponseModel>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userEntity = new UserEntity()
                {
                    UserName = request.UserName.ToLower().Trim(),
                    FullName = request.FullName,
                    CreatorUserId = request.UserProfile.UserSession.UserId,
                    Password = PasswordUtils.HashPassword(request.Password),
                    PasswordExpiryDate = request.ResetPassword ? DateTime.UtcNow.AddMonths(-1) : DateTime.UtcNow.AddMonths(6),
                };

                await _appDbContext.Users.AddAsync(userEntity, cancellationToken);

                await _appDbContext.SaveChangesAsync(cancellationToken);

                //"$argon2id$v=19$m=32768,t=4,p=1$nJHhhVTDjmY8jjq7shlZNw$Vy906ictTHOPowS5TdkhhGXJQLeH4mJb7Lp3JK3GS+I"
                
                return new()
                {
                    Status = true,
                    StatusMessage = "Ok",
                    Data = userEntity.UserName
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