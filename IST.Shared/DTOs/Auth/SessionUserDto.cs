using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

/// <summary>
/// DTO текущего авторизованного пользователя.
/// Используется для формирования ClaimsPrincipal в AuthenticationStateProvider.
/// </summary>
[DataContract]
public class SessionUserDto
{
    [DataMember] public Guid Id { get; set; }
    [DataMember] public string Login { get; set; } = "";
    [DataMember] public string FullName { get; set; } = "";
    [DataMember] public string Email { get; set; } = "";
    [DataMember] public bool IsActive { get; set; }
    [DataMember] public List<string> Roles { get; set; } = new();
}
