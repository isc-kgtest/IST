using ActualLab.CommandR;
using ActualLab.Fusion.Authentication;
using System.Runtime.Serialization;
using System.Reactive;

namespace IST.Contracts.Features.Auth;

[DataContract]
public record LoginTelegramCmd(
    [property: DataMember] ActualLab.Fusion.Session Session,
    [property: DataMember] string TelegramInitData
) : ICommand<Unit>;
