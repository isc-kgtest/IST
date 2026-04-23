using System.Security.Cryptography;
using Sodium;

namespace IST.Infrastructure.Security;

/// <summary>
/// Утилиты безопасной работы с паролями на базе Argon2id (libsodium).
/// Используется в АИС «Сертификация СЗИ» для хранения паролей пользователей
/// в соответствии с требованиями ИСПДн Уровня 3 и ТЗ п.3.3.9.
/// </summary>
public static class PasswordUtils
{
    /// <summary>Минимальная длина пароля согласно политике безопасности.</summary>
    public const int MinPasswordLength = 8;

    /// <summary>Максимальная длина пароля (ограничение Argon2 по энтропии).</summary>
    public const int MaxPasswordLength = 128;

    /// <summary>Минимальная длина сгенерированного пароля.</summary>
    public const int MinGeneratedLength = 12;

    // ===== Хэширование =====

    /// <summary>
    /// Создаёт хэш пароля с помощью Argon2id в стандартном формате MCF.
    /// Использует профиль Interactive (~100мс на хэш), оптимальный для веб-логина.
    /// </summary>
    /// <param name="password">Пароль в открытом виде.</param>
    /// <returns>Строка хэша, которую можно сохранить в БД (самодостаточная — содержит соль и параметры).</returns>
    /// <exception cref="ArgumentException">Если пароль пустой или превышает максимальную длину.</exception>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Пароль не может быть пустым.", nameof(password));

        if (password.Length > MaxPasswordLength)
            throw new ArgumentException(
                $"Длина пароля не должна превышать {MaxPasswordLength} символов.",
                nameof(password));

        return PasswordHash.ArgonHashString(password, PasswordHash.StrengthArgon.Interactive);
    }

    /// <summary>
    /// Проверяет пароль по сохранённому хэшу. 
    /// Метод никогда не бросает исключений — при любой ошибке возвращает false.
    /// </summary>
    /// <param name="password">Пароль, введённый пользователем.</param>
    /// <param name="hashedPassword">Хэш из базы данных.</param>
    /// <returns>true — пароль совпадает; false — не совпадает или хэш повреждён.</returns>
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            return false;

        if (password.Length > MaxPasswordLength)
            return false;

        try
        {
            return PasswordHash.ArgonHashStringVerify(hashedPassword, password);
        }
        catch
        {
            // Битый хэш в БД не должен валить систему.
            // Логируется на уровне вызывающего кода.
            return false;
        }
    }

    /// <summary>
    /// Проверяет, нужно ли пересчитать хэш с более сильными параметрами.
    /// Рекомендуется вызывать после успешного логина — если true, пересоздать хэш.
    /// </summary>
    public static bool NeedsRehash(string hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword))
            return true;

        try
        {
            return PasswordHash.ArgonPasswordNeedsRehash(
                hashedPassword,
                PasswordHash.StrengthArgon.Interactive);
        }
        catch
        {
            return true;
        }
    }

    // ===== Валидация сложности =====

    /// <summary>
    /// Проверяет соответствие пароля политике сложности.
    /// Политика: не менее 8 символов, заглавная + строчная буква + цифра + спецсимвол.
    /// </summary>
    public static PasswordValidationResult ValidateStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return new(false, "Пароль обязателен.");

        if (password.Length < MinPasswordLength)
            return new(false, $"Пароль должен содержать не менее {MinPasswordLength} символов.");

        if (password.Length > MaxPasswordLength)
            return new(false, $"Пароль не должен превышать {MaxPasswordLength} символов.");

        if (!password.Any(char.IsUpper))
            return new(false, "Пароль должен содержать хотя бы одну заглавную букву.");

        if (!password.Any(char.IsLower))
            return new(false, "Пароль должен содержать хотя бы одну строчную букву.");

        if (!password.Any(char.IsDigit))
            return new(false, "Пароль должен содержать хотя бы одну цифру.");

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return new(false, "Пароль должен содержать хотя бы один специальный символ.");

        // Защита от тривиальных паролей
        if (ContainsCommonPattern(password))
            return new(false, "Пароль слишком простой или содержит распространённую последовательность.");

        return new(true, null);
    }

    private static bool ContainsCommonPattern(string password)
    {
        var lower = password.ToLowerInvariant();
        string[] commonPatterns =
        [
            "password", "пароль", "qwerty", "йцукен", "123456", "111111",
            "admin", "user", "login", "welcome"
        ];

        return commonPatterns.Any(p => lower.Contains(p));
    }

    // ===== Генерация =====

    /// <summary>Наборы символов, из которых составляется случайный пароль.</summary>
    private const string LowercaseChars = "abcdefghijkmnopqrstuvwxyz";   // без 'l'
    private const string UppercaseChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";    // без 'I', 'O'
    private const string DigitChars = "23456789";                         // без '0', '1' (похожи на O, l)
    private const string SpecialChars = "!@#$%^&*()-_=+[]{};:,.<>?";

    /// <summary>
    /// Генерирует криптографически стойкий случайный пароль.
    /// Гарантированно содержит хотя бы по одному символу из каждого выбранного набора.
    /// Для удобства чтения исключены похожие символы (0/O, 1/l/I).
    /// </summary>
    /// <param name="length">Длина пароля. Минимум равен количеству включённых наборов.</param>
    /// <param name="useUppercase">Включать заглавные буквы.</param>
    /// <param name="useLowercase">Включать строчные буквы.</param>
    /// <param name="useDigits">Включать цифры.</param>
    /// <param name="useSpecialChars">Включать специальные символы.</param>
    public static string GenerateRandomPassword(
        int length = 16,
        bool useUppercase = true,
        bool useLowercase = true,
        bool useDigits = true,
        bool useSpecialChars = true)
    {
        var charSets = new List<string>();
        if (useLowercase) charSets.Add(LowercaseChars);
        if (useUppercase) charSets.Add(UppercaseChars);
        if (useDigits) charSets.Add(DigitChars);
        if (useSpecialChars) charSets.Add(SpecialChars);

        if (charSets.Count == 0)
            throw new InvalidOperationException("Должен быть включён хотя бы один набор символов.");

        // Длина не может быть меньше количества наборов — иначе не гарантируем присутствие каждого
        if (length < charSets.Count)
            throw new ArgumentException(
                $"Длина пароля должна быть не меньше {charSets.Count} (по количеству выбранных наборов символов).",
                nameof(length));

        if (length > MaxPasswordLength)
            throw new ArgumentException(
                $"Длина пароля не должна превышать {MaxPasswordLength}.",
                nameof(length));

        var passwordChars = new char[length];
        var fullPool = string.Concat(charSets);

        // Шаг 1: гарантируем по одному символу из каждого набора
        for (int i = 0; i < charSets.Count; i++)
        {
            var set = charSets[i];
            passwordChars[i] = set[RandomNumberGenerator.GetInt32(set.Length)];
        }

        // Шаг 2: добираем остальные символы из общего пула
        for (int i = charSets.Count; i < length; i++)
        {
            passwordChars[i] = fullPool[RandomNumberGenerator.GetInt32(fullPool.Length)];
        }

        // Шаг 3: перемешиваем, чтобы гарантированные символы не стояли в начале
        RandomNumberGenerator.Shuffle<char>(passwordChars);

        return new string(passwordChars);
    }
}

/// <summary>
/// Результат валидации пароля.
/// </summary>
public readonly record struct PasswordValidationResult(bool IsValid, string? ErrorMessage);