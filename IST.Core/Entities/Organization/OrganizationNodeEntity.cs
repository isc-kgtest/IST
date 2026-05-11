using IST.Core.Entities.BaseEntities;

namespace IST.Core.Entities.Organization;

/// <summary>
/// Узел оргструктуры. Самореферентное дерево — <see cref="ParentNodeId"/> указывает на родителя.
///
/// <see cref="Path"/> — материализованный путь от корня для эффективных подзапросов
/// «все потомки данного узла». Формат: <c>/{rootId}/{childId}/.../{thisId}/</c>.
/// Заполняется кодом при создании/перемещении узла.
/// </summary>
public partial class OrganizationNodeEntity : BaseEntity
{
    /// <summary>Тип узла (область/район/...). Можно менять.</summary>
    public Guid NodeTypeId { get; set; }
    public OrganizationNodeTypeEntity NodeType { get; set; } = null!;

    /// <summary>Родитель в дереве. Null — корневой узел.</summary>
    public Guid? ParentNodeId { get; set; }
    public OrganizationNodeEntity? ParentNode { get; set; }

    public ICollection<OrganizationNodeEntity> Children { get; set; } = new List<OrganizationNodeEntity>();

    /// <summary>Видимое имя узла ("Чуйская область", "Бишкек", "ОВД Ленинского района").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Опциональный код (для связи с внешними справочниками).</summary>
    public string? Code { get; set; }

    /// <summary>Материализованный путь для быстрых иерархических запросов.</summary>
    public string Path { get; set; } = "/";

    /// <summary>Глубина в дереве (0 — корень).</summary>
    public int Depth { get; set; }

    /// <summary>Порядок отображения среди sibling'ов.</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Description { get; set; }
}
