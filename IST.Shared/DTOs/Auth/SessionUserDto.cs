using MemoryPack;
using System.Runtime.Serialization;

namespace IST.Shared.DTOs.Auth;

[MemoryPackable]
public partial class SessionUserDto
{
    [MemoryPackOrder(0)]
    public Guid Id { get; set; }
    [MemoryPackOrder(1)]
    public string Login { get; set; } = "";
    [MemoryPackOrder(2)]
    public string FullName { get; set; } = "";
    [MemoryPackOrder(3)]
    public string Email { get; set; } = "";
    [MemoryPackOrder(4)]
    public bool IsActive { get; set; }
    [MemoryPackOrder(5)]
    public List<string> Roles { get; set; } = new();
}
