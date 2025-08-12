using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace BrandshareDamSync.Infrastructure.Secrets;

public sealed class SimpleSecretStore : ISecretStore
{
    private readonly string _dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "brandshare-dam-sync", "secrets");
    public SimpleSecretStore() { Directory.CreateDirectory(_dir); }

    public async Task SetAsync(string key, string secret, CancellationToken ct)
    {
        var dest = Path.Combine(_dir, key.Replace(":", "_"));
        var plain = System.Text.Encoding.UTF8.GetBytes(secret);
        byte[] protectedBytes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser)
            : ProtectPortable(plain);
        await File.WriteAllBytesAsync(dest, protectedBytes, ct);
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct)
    {
        var path = Path.Combine(_dir, key.Replace(":", "_"));
        if (!File.Exists(path)) return null;
        var bytes = await File.ReadAllBytesAsync(path, ct);
        var plain = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser)
            : UnprotectPortable(bytes);
        return System.Text.Encoding.UTF8.GetString(plain);
    }

    private static byte[] ProtectPortable(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = MachineKey();
        aes.GenerateIV();
        using var enc = aes.CreateEncryptor();
        var cipher = enc.TransformFinalBlock(data, 0, data.Length);
        return aes.IV.Concat(cipher).ToArray();
    }
    private static byte[] UnprotectPortable(byte[] blob)
    {
        using var aes = Aes.Create();
        aes.Key = MachineKey();
        aes.IV = blob.Take(16).ToArray();
        var cipher = blob.Skip(16).ToArray();
        using var dec = aes.CreateDecryptor();
        return dec.TransformFinalBlock(cipher, 0, cipher.Length);
    }
    private static byte[] MachineKey()
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Environment.MachineName));
    }
}
