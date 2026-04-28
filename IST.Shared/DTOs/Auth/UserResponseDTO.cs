using MemoryPack;
using System.Runtime.Serialization;
using Mapster;
using IST.Core.Entities.Auth;

namespace IST.Shared.DTOs.Auth;

[DataContract]
[MemoryPackable]
public partial class UserResponseDTO : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserEntity, UserResponseDTO>();
    }
    [DataMember, MemoryPackOrder(0)]
    public Guid Id { get; set; }

    [DataMember, MemoryPackOrder(1)]
    public string Login { get; set; } = "";

    [DataMember, MemoryPackOrder(2)]
    public string FullName { get; set; } = "";
}
