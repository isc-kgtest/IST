using ActualLab.Fusion;
using IST.Shared.DTOs.Common;
using MemoryPack;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace IST.Contracts.Features.Auth.Commands;

[DataContract]
[MemoryPackable]
public partial record DeleteUserCommand(
    [property: DataMember] Session Session,
    [Required(ErrorMessage = "Id обязательно.")]
    [property: DataMember] Guid UserId
) : ICommand<ResponseDTO<string>>;
