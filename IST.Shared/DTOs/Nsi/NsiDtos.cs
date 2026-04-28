using System;

namespace IST.Shared.DTOs.Nsi;

public class NsiItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }
}

public class UpsertNsiItemRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// Для регионов и районов
public class RegionDto : NsiItemDto
{
    public Guid CountryId { get; set; }
}
public class UpsertRegionRequest : UpsertNsiItemRequest
{
    public Guid CountryId { get; set; }
}

public class DistrictDto : NsiItemDto
{
    public Guid RegionId { get; set; }
}
public class UpsertDistrictRequest : UpsertNsiItemRequest
{
    public Guid RegionId { get; set; }
}
