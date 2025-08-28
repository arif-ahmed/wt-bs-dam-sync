
namespace BrandshareDamSync.Infrastructure;

public static class FileHelpers
{
    internal static void MoveFileOverwrite(string source, string dest)
    {
        if (string.Equals(Path.GetFullPath(source), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
            return;

        if (File.Exists(dest))
            File.Delete(dest);

        // Ensure destination directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

        File.Move(source, dest);
    }
}

