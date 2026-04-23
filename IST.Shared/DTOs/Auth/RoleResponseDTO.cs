using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

[DataContract]
public class RoleResponseDTO
{
    [DataMember] public Guid Id { get; set; }
    [DataMember] public string Name { get; set; } = "";
    [DataMember] public string Description { get; set; } = "";
    [DataMember] public bool Disabled { get; set; }
}