using ActualLab.Fusion;
using IST.Shared.DTOs.Audit;

namespace IST.Contracts.Features.Audit;

public interface IAuditQueries : IComputeService
{
    [ComputeMethod(MinCacheDuration = 30)]
    Task<AuditLogPageDto> GetAuditPageAsync(
        Session session, AuditFilter filter, int skip, int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Список distinct event-types, реально встречающихся в журнале.
    /// Нужен для выпадашки фильтра.
    /// </summary>
    [ComputeMethod(MinCacheDuration = 60)]
    Task<List<string>> GetEventTypesAsync(Session session, CancellationToken cancellationToken = default);
}
