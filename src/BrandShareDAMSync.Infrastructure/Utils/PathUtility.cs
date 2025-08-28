namespace BrandshareDamSync.Infrastructure.Utils;

public static class PathUtility
{
    /// <summary>
    /// Builds a safe local path from an API-provided folder path and file name.
    /// Ensures the directory exists before returning.
    /// </summary>
    /// <param name="rootFolder">The local root folder where files should be stored.</param>
    /// <param name="apiFolderPath">Folder path from API (e.g. "//SCANS_LOGOS/old").</param>
    /// <param name="fileName">The file name (e.g. "logo.png").</param>
    /// <returns>Full path on the local file system.</returns>
    public static string BuildLocalPath(string rootFolder, string? apiFolderPath, string fileName)
    {
        // Default to just fileName if API path is empty/null
        if (string.IsNullOrWhiteSpace(apiFolderPath))
            apiFolderPath = string.Empty;

        // Step 1: Normalise separators to OS convention
        var clean = apiFolderPath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        // Step 2: Remove any leading directory separators ("//SCANS..." -> "SCANS...")
        clean = clean.TrimStart(Path.DirectorySeparatorChar);

        // Step 3: Combine safely
        var fullPath = Path.Combine(rootFolder, clean, fileName);

        // Step 4: Ensure directory exists
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        return fullPath;
    }
}
