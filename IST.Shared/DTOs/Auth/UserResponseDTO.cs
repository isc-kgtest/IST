using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

[DataContract]
public class UserResponseDTO
{
    [DataMember]
    public Guid Id { get; set; }

    [DataMember]
    public string Login { get; set; } = "";

    [DataMember]
    public string FullName { get; set; } = "";
}
