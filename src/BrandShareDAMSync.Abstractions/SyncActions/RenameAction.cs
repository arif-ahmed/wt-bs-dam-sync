using BrandshareDamSync.Domain;

namespace BrandshareDamSync.Abstractions.SyncActions;

public sealed class RenameAction : ISyncAction 
{
    public SyncActionType ActionType => SyncActionType.Rename;

    public void Execute(SyncContext ctx)
    {
        var target = Path.Combine(ctx.LocalDirectory, ctx.RemoteFileName);
        Directory.CreateDirectory(ctx.LocalDirectory);
        Console.WriteLine($"[Rename] {ctx.LocalFilePath} -> {target}");
        // FileHelpers.MoveFileOverwrite(ctx.LocalFilePath, target);
    }
}
