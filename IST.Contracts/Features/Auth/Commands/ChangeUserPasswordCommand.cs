namespace IST.Contracts.Features.Auth.Commands;
using MemoryPack;

[DataContract]
[MemoryPackable]
public partial record ChangeUserPasswordCommand(
    [property: DataMember] Session Session,
    [property: DataMember] ChangeUserPasswordRequest Request
) : ICommand<ResponseDTO<string>>;
