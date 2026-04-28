using MemoryPack;
using System.Runtime.Serialization;
using Mapster;
using IST.Core.Entities.Auth;

namespace IST.Shared.DTOs.Auth;

[DataContract]
[MemoryPackable]
public partial class RoleResponseDTO : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RoleEntity, RoleResponseDTO>();
    }
    [DataMember, MemoryPackOrder(0)] public Guid Id { get; set; }
    [DataMember, MemoryPackOrder(1)] public string Name { get; set; } = "";
    [DataMember, MemoryPackOrder(2)] public string Description { get; set; } = "";
    [DataMember, MemoryPackOrder(3)] public bool Disabled { get; set; }
}
