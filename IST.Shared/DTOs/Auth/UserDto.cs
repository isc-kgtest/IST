using MemoryPack;
using System.ComponentModel.DataAnnotations;

namespace IST.Shared.DTOs.Auth;

[MemoryPackable]
public partial class UserDto
{
    [MemoryPackOrder(0)]
    public Guid Id { get; set; }
    
    [MemoryPackOrder(1)]
    public string Surname { get; set; } = string.Empty;
    
    [MemoryPackOrder(2)]
    public string Name { get; set; } = string.Empty;
    
    [MemoryPackOrder(3)]
    public string? Patronymic { get; set; }

    [MemoryPackOrder(4)]
    public string FullName { get; set; } = string.Empty;

    [MemoryPackOrder(5)]
    public string Position { get; set; } = string.Empty;
    
    [MemoryPackOrder(6)]
    public string Department { get; set; } = string.Empty;
    
    [MemoryPackOrder(7)]
    public Guid OrganizationId { get; set; }

    [MemoryPackOrder(8)]
    public string EMail { get; set; } = string.Empty;
    
    [MemoryPackOrder(9)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MemoryPackOrder(10)]
    public string Login { get; set; } = string.Empty;

    [MemoryPackOrder(11)]
    public DateTime PasswordExpiryDate { get; set; }
    
    [MemoryPackOrder(12)]
    public DateTime? LastDateLogin { get; set; }
    
    [MemoryPackOrder(13)]
    public bool IsActive { get; set; }
    
    [MemoryPackOrder(14)]
    public DateTime CreatedAt { get; set; }
    
    [MemoryPackOrder(15)]
    public bool IsDeleted { get; set; }

    [MemoryPackOrder(16)]
    public List<UserRoleResponseDTO> UserRoles { get; set; } = new();
}
