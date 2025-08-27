using Amazon;

namespace BrandshareDamSync.Infrastructure.S3;

public static class RegionMapper
{
    public static RegionEndpoint FromHint(string hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return RegionEndpoint.USEast1;

        hint = hint.Trim().ToLowerInvariant();

        // Full codes first
        try
        {
            // If it's a full region name Amazon understands, use it.
            var endpoint = RegionEndpoint.GetBySystemName(hint);
            if (endpoint != null) return endpoint;
        }
        catch { /* fall back */ }

        // Shorthand hints
        return hint switch
        {
            "eu" => RegionEndpoint.EUWest1,     // adjust if you prefer eu-central-1
            "us" => RegionEndpoint.USEast1,
            "ap" => RegionEndpoint.APSouth1,    // Dhaka-adjacent? often AP-South-1 (Mumbai)
            "apac" => RegionEndpoint.APSoutheast1,
            _ => RegionEndpoint.USEast1
        };
    }
}
