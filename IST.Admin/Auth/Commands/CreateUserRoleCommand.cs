using ASIO10.Application.Common.Attributes;
using ASIO10.Application.Common.Interfaces;
using ASIO10.Auth.Models;
using ASIO10.Domain.EntityModels.Auth;
using System.ComponentModel.DataAnnotations;

namespace ASIO10.Auth.Queries;

using ResponseModel = ASIO10.Domain.EntityModels.Auth.UserRolesEntity;

[AuthRole("admin")]
public class CreateUserRoleCommand : IRequest<ResponseDTO<ResponseModel>>, IAuthorizableRequest
{
    [Required(ErrorMessage = "Id обязательно.")]
    public Ulid UserId { get; set; }

    [Required(ErrorMessage = "Id обязательно.")]
    public Ulid RoleId { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public required UserProfile UserProfile { get; set; }

    public class Handler : IRequestHandler<CreateUserRoleCommand, ResponseDTO<ResponseModel>>
    {
        private readonly IAppDbContext _appDbContext;

        public Handler(IAppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<ResponseDTO<ResponseModel>> Handle(CreateUserRoleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userRole = new UserRolesEntity()
                {
                    CreatorUserId = request.UserProfile.UserSession.UserId,
                    UserId = request.UserId,
                    RoleId = request.RoleId,
                    StartDate = request.StartDate ?? DateTime.UtcNow,
                    EndDate = request.EndDate ?? DateTime.MinValue,
                };

                await _appDbContext.UserRoles.AddAsync(userRole, cancellationToken);

                await _appDbContext.SaveChangesAsync(cancellationToken);

                return new()
                {
                    Status = true,
                    StatusMessage = "Ok",
                    Data = userRole
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