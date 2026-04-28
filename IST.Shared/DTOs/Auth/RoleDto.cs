using MemoryPack;
using Mapster;
using IST.Core.Entities.Auth;

namespace IST.Shared.DTOs.Auth;

[MemoryPackable]
public partial class RoleDto : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RoleEntity, RoleDto>();
    }
    [MemoryPackOrder(0)]
    public Guid Id { get; set; }
    
    [MemoryPackOrder(1)]
    public string Name { get; set; } = string.Empty;
    
    [MemoryPackOrder(2)]
    public string? Description { get; set; }

    [MemoryPackOrder(3)]
    public DateTime CreatedAt { get; set; }

    [MemoryPackOrder(4)]
    public bool IsDeleted { get; set; }
}
