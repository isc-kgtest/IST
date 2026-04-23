
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

[DataContract]
public class UpdateUserRoleRequest
{
    [DataMember, Required]
    public Guid Id { get; set; }

    [DataMember, Required]
    public DateTime StartDate { get; set; }

    [DataMember]
    public DateTime? EndDate { get; set; }
}