using Amazon;

namespace BrandshareDamSync.Infrastructure.S3;

public sealed record ParsedToken(string BaseDirectory, RegionEndpoint Region, string AccessKeyId, string SecretKey);

public static class TokenParser
{
    public static ParsedToken Parse(string token)
    {
        var parts = token?.Split('_') ?? Array.Empty<string>();
        if (parts.Length < 4) throw new FormatException("Token must have at least 4 underscore-separated parts.");

        string baseDir = parts[1];
        string regionHint = parts[2];

        // Join the remainder and split once for AKIA... and secret
        string creds = string.Join("_", parts, 3, parts.Length - 3);
        int idx = creds.IndexOf('_');
        if (idx <= 0 || idx == creds.Length - 1)
            throw new FormatException("Credential part must contain AccessKeyId and SecretKey separated by an underscore.");

        string accessKeyId = creds[..idx];
        string secretKey = creds[(idx + 1)..];

        // Map region hint
        var region = MapRegion(regionHint);

        return new ParsedToken(baseDir, region, accessKeyId, secretKey);
    }

    private static RegionEndpoint MapRegion(string hint)
    {
        if (string.IsNullOrWhiteSpace(hint)) return RegionEndpoint.USEast1;

        // Try full region code first (e.g., eu-west-1)
        try { return RegionEndpoint.GetBySystemName(hint); } catch { /* fall back */ }

        // Shorthand fallbacks (adjust to your estate)
        return hint.Trim().ToLowerInvariant() switch
        {
            "eu" => RegionEndpoint.EUWest1,
            "us" => RegionEndpoint.USEast1,
            "ap" => RegionEndpoint.APSouth1,
            _ => RegionEndpoint.USEast1
        };
    }
}
