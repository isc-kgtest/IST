using MemoryPack;
using System.Runtime.Serialization;
using Mapster;
using IST.Core.Entities.Auth;

namespace IST.Shared.DTOs.Auth;

[DataContract]
[MemoryPackable]
public partial class UserRoleResponseDTO : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserRolesEntity, UserRoleResponseDTO>()
            .Map(dest => dest.UserFullName, src => src.User.FullName)
            .Map(dest => dest.RoleName, src => src.Role.Name);
    }
    [DataMember, MemoryPackOrder(0)] public Guid Id { get; set; }
    [DataMember, MemoryPackOrder(1)] public Guid UserId { get; set; }
    [DataMember, MemoryPackOrder(2)] public string UserFullName { get; set; } = "";
    [DataMember, MemoryPackOrder(3)] public Guid RoleId { get; set; }
    [DataMember, MemoryPackOrder(4)] public string RoleName { get; set; } = "";
    [DataMember, MemoryPackOrder(5)] public DateTime StartDate { get; set; }
    [DataMember, MemoryPackOrder(6)] public DateTime? EndDate { get; set; }
}
