namespace BrandshareDamSync.Infrastructure.Utils;

public class PathStandardizer
{
    /// <summary>
    /// Creates a standardized, root-relative path for storage (e.g., in a database or DAM).
    /// This format always uses forward slashes and starts with a leading slash.
    /// Correctly returns "/" when the path IS the root.
    /// </summary>
    public static string CreateStandardizedRelativePath(string rootPath, string absolutePath)
    {
        // 1. Get the base relative path.
        string relativePath = Path.GetRelativePath(rootPath, absolutePath);

        // 2. Handle the edge case where the paths are the same.
        if (relativePath == ".")
        {
            return "/";
        }

        // 3. Normalize to use forward slashes for universal compatibility.
        string forwardSlashPath = relativePath.Replace(Path.DirectorySeparatorChar, '/');

        // 4. Prepend the leading slash for all other sub-directories/files.
        return "/" + forwardSlashPath;
    }

    /// <summary>
    /// Converts a standardized relative path back into a full, OS-specific absolute path.
    /// </summary>
    public static string GetAbsoluteOsPath(string rootPath, string standardizedPath)
    {
        string relativePart = standardizedPath.TrimStart('/');
        return Path.Combine(rootPath, relativePart);
    }
}
