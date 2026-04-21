using System;
using System.Text;
using System.Security.Cryptography;
using Sodium; // Пространство имен из пакета Sodium.Core

/// <summary>
/// Утилитарный класс для безопасной работы с паролями на основе стандарта Argon2id
/// через библиотеку Sodium.Core, которая является современной оберткой для Libsodium.
/// </summary>
public static class PasswordUtils
{
    /// <summary>
    /// Создает хеш пароля с помощью Argon2id, используя библиотеку Sodium.Core.
    /// </summary>
    /// <param name="password">Пароль для хеширования.</param>
    /// <returns>Строка с хешем в стандартном формате Modular Crypt Format (MCF).</returns>
    public static string HashPassword(string password)
    {
        // Используем метод ArgonHashString из Sodium.Core.
        // По умолчанию он использует самый надежный вариант - Argon2id.
        // Второй параметр - это профиль безопасности. StrengthArgon.Interactive
        // является рекомендуемым для интерактивных систем (например, логин на сайте).
        return PasswordHash.ArgonHashString(password, PasswordHash.StrengthArgon.Interactive);
    }

    /// <summary>
    /// Проверяет пароль по хешу, созданному с помощью Sodium.Core.
    /// </summary>
    /// <param name="password">Пароль, введенный пользователем для проверки.</param>
    /// <param name="hashedPassword">Строка с хешем из базы данных.</param>
    /// <returns>True, если пароль верный, иначе False.</returns>
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        // Используем соответствующий метод для проверки из Sodium.Core.
        // Он автоматически извлекает из строки хеша все параметры (соль, память, итерации)
        // и производит безопасное сравнение.
        return PasswordHash.ArgonHashStringVerify(hashedPassword, password);
    }

    /// <summary>
    /// Генерирует криптографически стойкий случайный пароль.
    /// Этот метод не зависит от Sodium и использует встроенные в .NET инструменты.
    /// </summary>
    /// <param name="length">Длина пароля.</param>
    /// <param name="useUppercase">Включать ли заглавные буквы.</param>
    /// <param name="useLowercase">Включать ли строчные буквы.</param>
    /// <param name="useDigits">Включать ли цифры.</param>
    /// <param name="useSpecialChars">Включать ли спецсимволы.</param>
    /// <returns>Сгенерированный пароль.</returns>
    public static string GenerateRandomPassword(int length = 16, bool useUppercase = true, bool useLowercase = true, bool useDigits = true, bool useSpecialChars = true)
    {
        const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digitChars = "0123456789";
        const string specialChars = @"!@#$%^&*()";

        var charPool = new StringBuilder();
        var passwordBuilder = new StringBuilder();

        if (length <= 0) return string.Empty;

        var charSets = new List<string>();
        if (useLowercase) charSets.Add(lowercaseChars);
        if (useUppercase) charSets.Add(uppercaseChars);
        if (useDigits) charSets.Add(digitChars);
        if (useSpecialChars) charSets.Add(specialChars);

        if (charSets.Count == 0) throw new InvalidOperationException("Не выбрано ни одного набора символов.");

        // Гарантируем наличие хотя бы одного символа из каждого набора
        foreach (var charSet in charSets)
        {
            if (passwordBuilder.Length < length)
            {
                passwordBuilder.Append(charSet[RandomNumberGenerator.GetInt32(charSet.Length)]);
                charPool.Append(charSet);
            }
        }

        // Заполняем остаток пароля
        for (var i = passwordBuilder.Length; i < length; i++)
        {
            passwordBuilder.Append(charPool[RandomNumberGenerator.GetInt32(charPool.Length)]);
        }

        var passwordChars = passwordBuilder.ToString().ToCharArray();
        RandomNumberGenerator.Shuffle<char>(passwordChars);

        return new string(passwordChars);
    }
}