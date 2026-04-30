using System.Security.Cryptography;
using System.Text;

namespace HoReCa.Infrastructure.Services;

public static class PasswordHelper
{
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + "_HoReCa_Salt_2026"));
        return Convert.ToHexString(bytes);
    }

    public static bool VerifyPassword(string password, string hash)
    {
        var inputHash = HashPassword(password);
        return string.Equals(inputHash, hash, StringComparison.OrdinalIgnoreCase);
    }
}
