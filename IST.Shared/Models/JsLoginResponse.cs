using IST.Shared.Enums;
namespace IST.Shared.Models;

public class JsLoginResponse
{
    public bool Ok { get; set; }
    public JsResponseDto? ResponseDto { get; set; } // Здесь будет обертка для десериализации DTO

    // Вложенный класс для десериализации ResponseDto
    public class JsResponseDto
    {
        public object? Data { get; set; } // Используем object
        public bool Status { get; set; }
        public ResponseStatusCode StatusCode { get; set; } // ИЗМЕНЕНО: теперь это int!
        public string StatusMessage { get; set; } = string.Empty;
    }
}
