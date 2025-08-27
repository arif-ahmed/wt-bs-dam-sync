
using System.Security.Cryptography;

namespace BrandshareDamSync.Infrastructure.Utils;

public class FileUtils
{
    public static async Task<string> Sha256Async(string path)
    {
        using var fs = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 1024 * 1024, options: FileOptions.SequentialScan);

        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(fs);
        return Convert.ToHexString(hash);
    }

    public static string GetRelative(string basePath, string targetPath)
    {
        string rel = Path.GetRelativePath(basePath, targetPath);
        return rel == "." ? "/" : "/" + rel.Replace('\\', '/');
    }
}
