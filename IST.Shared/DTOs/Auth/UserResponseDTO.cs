using MemoryPack;
using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

[DataContract]
[MemoryPackable]
public partial class UserResponseDTO
{
    [DataMember, MemoryPackOrder(0)]
    public Guid Id { get; set; }

    [DataMember, MemoryPackOrder(1)]
    public string Login { get; set; } = "";

    [DataMember, MemoryPackOrder(2)]
    public string FullName { get; set; } = "";
}
