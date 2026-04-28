using IST.Core.Entities.BaseEntities;
using MemoryPack;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace IST.Core.Entities.Nsi;

// Базовый класс для простых справочников НСИ
public abstract class NsiBaseEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

[MemoryPackable]
public partial class SziTypeEntity : NsiBaseEntity { }

[MemoryPackable]
public partial class TrustLevelEntity : NsiBaseEntity { }

[MemoryPackable]
public partial class SecurityRequirementEntity : NsiBaseEntity { }

[MemoryPackable]
public partial class CertificationSchemeEntity : NsiBaseEntity { }

[MemoryPackable]
public partial class ApprovingBodyEntity : NsiBaseEntity { }

[MemoryPackable]
public partial class TestLaboratoryEntity : NsiBaseEntity { }

[MemoryPackable]
public partial class ApplicationStatusEntity : NsiBaseEntity { }

[MemoryPackable]
public partial class CertificateStatusEntity : NsiBaseEntity { }

[MemoryPackable]
public partial class CountryEntity : NsiBaseEntity { }

[MemoryPackable]
public partial class CurrencyEntity : NsiBaseEntity { }

[MemoryPackable]
public partial class RegionEntity : NsiBaseEntity
{
    public Guid CountryId { get; set; }
}

[MemoryPackable]
public partial class DistrictEntity : NsiBaseEntity
{
    public Guid RegionId { get; set; }
}
