using IST.Core.Entities.BaseEntities;

namespace IST.Core.Entities.Organization;

/// <summary>
/// Тип узла оргструктуры. Полностью динамический — админ создаёт и переименовывает
/// типы сам. По умолчанию seed создаёт типовой набор: Область / Район / Город /
/// Министерство / Управление / Отдел.
/// </summary>
public partial class OrganizationNodeTypeEntity : BaseEntity
{
    /// <summary>Машинно-читаемый код, например "region", "district".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Видимое имя ("Область", "Район").</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Глубина по умолчанию: 0 = верхний (корневой) уровень.</summary>
    public int Level { get; set; }

    /// <summary>Порядок отображения в выпадашках/списках.</summary>
    public int SortOrder { get; set; }

    /// <summary>Иконка (Material Symbols name) для UI.</summary>
    public string? Icon { get; set; }

    public ICollection<OrganizationNodeEntity> Nodes { get; set; } = new List<OrganizationNodeEntity>();
}
