using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;


[DataContract]
public class UserRoleResponseDTO
{
    [DataMember] public Guid Id { get; set; }
    [DataMember] public Guid UserId { get; set; }
    [DataMember] public string UserFullName { get; set; } = "";
    [DataMember] public Guid RoleId { get; set; }
    [DataMember] public string RoleName { get; set; } = "";
    [DataMember] public DateTime StartDate { get; set; }
    [DataMember] public DateTime? EndDate { get; set; }
}