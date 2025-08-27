
namespace BrandshareDamSync.Abstractions;

public record SyncContext(
    string LocalRoot,
    string LocalFilePath,
    string RemotePath,
    string RemoteFileName
)
{
    public string LocalDirectory => Path.GetDirectoryName(LocalFilePath) ?? string.Empty;
    public string LocalFileName => Path.GetFileName(LocalFilePath);
    public string ExpectedLocalDir => Path.Combine(LocalRoot, RemotePath);
    public string ExpectedLocalPath => Path.Combine(ExpectedLocalDir, RemoteFileName);
}
