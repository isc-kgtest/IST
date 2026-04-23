using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;
[DataContract]
public class CreateUserRoleRequest
{
    [DataMember, Required]
    public Guid UserId { get; set; }

    [DataMember, Required]
    public Guid RoleId { get; set; }

    /// <summary>Дата начала действия роли. Если не указана — текущий момент.</summary>
    [DataMember]
    public DateTime? StartDate { get; set; }

    /// <summary>Дата окончания. null = постоянная роль.</summary>
    [DataMember]
    public DateTime? EndDate { get; set; }
}