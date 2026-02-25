using System.Security.Cryptography;
using System.Text;

namespace PhotoSortingApp.Core.Infrastructure;

public static class Hashing
{
    public static string ComputeSha256ForFile(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }

    public static string ComputeSha1ForText(string value)
    {
        using var sha1 = SHA1.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = sha1.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
