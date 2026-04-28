using ActualLab.Fusion;
using IST.Shared.DTOs.Nsi;

namespace IST.Contracts.Features.Nsi;

public interface INsiQueries : IComputeService
{
    [ComputeMethod]
    Task<Dictionary<string, string>> GetNsiTypesAsync(CancellationToken cancellationToken = default);

    // Обобщенный метод получения списка для любого типа НСИ
    // typeName определяет конкретный справочник
    [ComputeMethod]
    Task<List<NsiItemDto>> GetNsiItemsAsync(string typeName, CancellationToken cancellationToken = default);

    // Специфичные методы для иерархических справочников
    [ComputeMethod]
    Task<List<RegionDto>> GetRegionsAsync(CancellationToken cancellationToken = default);

    [ComputeMethod]
    Task<List<DistrictDto>> GetDistrictsAsync(CancellationToken cancellationToken = default);
}
