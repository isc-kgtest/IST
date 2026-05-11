using ActualLab.Fusion;
using IST.Shared.DTOs.Organization;

namespace IST.Contracts.Features.Organization;

public interface IOrganizationQueries : IComputeService
{
    [ComputeMethod(MinCacheDuration = 60)]
    Task<List<OrganizationNodeTypeDto>> GetNodeTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>Прямые потомки указанного узла. Если parentId = null — корневые узлы.</summary>
    [ComputeMethod(MinCacheDuration = 60)]
    Task<List<OrganizationNodeDto>> GetChildrenAsync(Guid? parentId, CancellationToken cancellationToken = default);

    [ComputeMethod(MinCacheDuration = 60)]
    Task<OrganizationNodeDto?> GetNodeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Все узлы плоским списком с готовой строкой пути — для выпадашек.</summary>
    [ComputeMethod(MinCacheDuration = 60)]
    Task<List<OrganizationNodeFlatDto>> GetAllNodesFlatAsync(CancellationToken cancellationToken = default);
}
