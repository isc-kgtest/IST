using ActualLab.Fusion;
using IST.Contracts.Features.Nsi;
using IST.Shared.DTOs.Nsi;

namespace IST.Services.Features.Nsi;

public class NsiQueries : INsiQueries
{
    public virtual Task<Dictionary<string, string>> GetNsiTypesAsync(CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string>
        {
            { "SziType", "Типы СЗИ" },
            { "TrustLevel", "Уровни доверия" },
            { "SecurityRequirement", "Требования по безопасности" },
            { "CertificationScheme", "Схемы сертификации" },
            { "ApprovingBody", "Согласующие органы" },
            { "TestLaboratory", "Испытательные лаборатории" },
            { "ApplicationStatus", "Статусы заявок" },
            { "CertificateStatus", "Статусы сертификатов" },
            { "Country", "Страны" },
            { "Currency", "Валюты" },
            { "Region", "Области" },
            { "District", "Районы" }
        };
        return Task.FromResult(dict);
    }

    public virtual Task<List<NsiItemDto>> GetNsiItemsAsync(string typeName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<NsiItemDto>());
    }

    public virtual Task<List<RegionDto>> GetRegionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<RegionDto>());
    }

    public virtual Task<List<DistrictDto>> GetDistrictsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<DistrictDto>());
    }
}
