using IST.Shared.Enums;

namespace IST.Shared.Interface;

public interface IResponseDTO
{
    bool Status { get; set; }
    string StatusMessage { get; set; }
    ResponseStatusCode StatusCode { get; set; }
}