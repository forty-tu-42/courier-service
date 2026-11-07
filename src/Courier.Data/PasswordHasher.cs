using System.Security.Cryptography;
using System.Text;

namespace Courier.Data;

/// <summary>
/// Хеширование паролей: SHA-256(соль + пароль) в кодировке UTF-16LE.
/// Такая же формула используется в SQL-скрипте при заполнении базы.
/// </summary>
public static class PasswordHasher
{
    public static string Hash(string password, string salt)
    {
        var bytes = Encoding.Unicode.GetBytes(salt + password);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    public static string GenerateSalt()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(8));

    public static bool Verify(string password, string salt, string expectedHash)
        => string.Equals(Hash(password, salt), expectedHash, StringComparison.OrdinalIgnoreCase);
}
