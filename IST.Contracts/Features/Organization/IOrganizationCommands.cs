using IST.Contracts.Features.Organization.Commands;
using IST.Shared.DTOs.Common;
using IST.Shared.DTOs.Organization;

namespace IST.Contracts.Features.Organization;

public interface IOrganizationCommands : ICommandService, IComputeService
{
    Task<ResponseDTO<OrganizationNodeTypeDto>> SaveNodeTypeAsync(SaveNodeTypeCommand command, CancellationToken cancellationToken = default);
    Task<ResponseDTO<string>> DeleteNodeTypeAsync(DeleteNodeTypeCommand command, CancellationToken cancellationToken = default);

    Task<ResponseDTO<OrganizationNodeDto>> SaveNodeAsync(SaveNodeCommand command, CancellationToken cancellationToken = default);
    Task<ResponseDTO<string>> DeleteNodeAsync(DeleteNodeCommand command, CancellationToken cancellationToken = default);
}
