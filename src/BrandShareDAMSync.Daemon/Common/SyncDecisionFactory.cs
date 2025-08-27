using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Domain;

namespace BrandshareDamSync.Daemon.Common;

public class SyncDecisionFactory
{
    public static SyncDecision Decide(SyncContext ctx)
    {
        // Case 1: Path + Name match ? if file exists locally, re-download (remote changed)
        if (PathsEqual(ctx.LocalFilePath, ctx.ExpectedLocalPath))
        {
            if (File.Exists(ctx.LocalFilePath))
                return new SyncDecision(SyncActionType.Redownload, "Path and name match; refresh from DAM.");

            // If it doesn't exist, moving/renaming isn't relevant; treat as NoOp
            return new SyncDecision(SyncActionType.NoOp, "Expected file not found locally.");
        }

        // Case 2: Path matches but name differs ? rename
        if (PathsEqual(ctx.LocalDirectory, ctx.ExpectedLocalDir) &&
            !NamesEqual(ctx.LocalFileName, ctx.RemoteFileName) &&
            File.Exists(ctx.LocalFilePath))
        {
            return new SyncDecision(SyncActionType.Rename, "Directory matches; file name differs.");
        }

        // Case 3: Path differs ? move
        if (!PathsEqual(ctx.LocalDirectory, ctx.ExpectedLocalDir) &&
            File.Exists(ctx.LocalFilePath))
        {
            return new SyncDecision(SyncActionType.Move, "Directory differs; move locally.");
        }

        return new SyncDecision(SyncActionType.NoOp, "No action required.");
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

    private static bool NamesEqual(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
