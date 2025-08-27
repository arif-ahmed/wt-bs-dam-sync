using BrandshareDamSync.Domain;

namespace BrandshareDamSync.Abstractions.SyncActions;

public sealed class RedownloadAction : ISyncAction
{
    public SyncActionType ActionType => SyncActionType.Redownload;

    public void Execute(SyncContext ctx)
    {
        Directory.CreateDirectory(ctx.ExpectedLocalDir);
        Console.WriteLine($"[Redownload] {ctx.ExpectedLocalPath}");
        // Replace with your actual download implementation:
        DownloadFile(ctx.RemotePath, ctx.RemoteFileName, ctx.ExpectedLocalPath);
    }

    private static void DownloadFile(string remotePath, string remoteFileName, string destinationPath)
    {
        // TODO: Call your DAM SDK/API here
        // Example stub:
        File.WriteAllText(destinationPath, $"Stub content for {remotePath}/{remoteFileName} @ {DateTime.UtcNow:O}");
    }
}
