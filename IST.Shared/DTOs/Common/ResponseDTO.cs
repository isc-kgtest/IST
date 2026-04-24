using MemoryPack;
using IST.Shared.Enums;
using IST.Shared.Interface;

namespace IST.Shared.DTOs.Common;

[MemoryPackable]
public partial class ResponseDTO<T> : IResponseDTO
{
    [MemoryPackOrder(0)]
    public T? Data { get; set; }
    [MemoryPackOrder(1)]
    public bool Status { get; set; }
    [MemoryPackOrder(2)]
    public ResponseStatusCode StatusCode { get; set; }
    [MemoryPackOrder(3)]
    public string StatusMessage { get; set; } = string.Empty;

    // Успешный результат
    public static ResponseDTO<T> Success(T data, string message = "Успешно")
    {
        return new ResponseDTO<T>
        {
            Data = data,
            Status = true,
            StatusCode = ResponseStatusCode.Ok,
            StatusMessage = message,
        };
    }

    // Успешный результат
    public static ResponseDTO<T> Ok(T data, string message = "Успешно")
    {
        return new ResponseDTO<T>
        {
            Data = data,
            Status = true,
            StatusCode = ResponseStatusCode.Ok,
            StatusMessage = message
        };
    }

    public static ResponseDTO<T> NotFound(string message = "Запрашиваемый ресурс не найден", ResponseStatusCode code = ResponseStatusCode.NotFound)
    {
        return new ResponseDTO<T>
        {
            Status = false,
            StatusCode = code,
            StatusMessage = message
        };
    }

    public static ResponseDTO<T> BadRequest(string message = "BadRequest", ResponseStatusCode code = ResponseStatusCode.BadRequest)
    {
        return new ResponseDTO<T>
        {
            Status = false,
            StatusCode = code,
            StatusMessage = message
        };
    }
    // Ошибка
    public static ResponseDTO<T> Fail(string message, ResponseStatusCode code = ResponseStatusCode.ValidationError)
    {
        return new ResponseDTO<T>
        {
            Status = false,
            StatusCode = code,
            StatusMessage = message
        };
    }

    public static ResponseDTO<T> InternalServerError(string message, ResponseStatusCode code = ResponseStatusCode.InternalServerError)
    {
        return new ResponseDTO<T>
        {
            Status = false,
            StatusCode = code,
            StatusMessage = message
        };
    }

    public static ResponseDTO<T> Conflict(string message = "Конфликт: ресурс уже существует.", ResponseStatusCode code = ResponseStatusCode.Conflict)
    {
        return new ResponseDTO<T>
        {
            Status = false,
            StatusCode = code,
            StatusMessage = message
        };
    }
}