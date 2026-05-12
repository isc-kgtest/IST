
namespace ASIO10.Application.Common.Models;
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string Error { get; set; } = string.Empty;
    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Fail(string error) => new() { IsValid = false, Error = error };
}