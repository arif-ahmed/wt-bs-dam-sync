using BrandshareDamSync.Domain;

namespace BrandshareDamSync.Abstractions.SyncActions;

public sealed class NoOpAction : ISyncAction 
{
    public SyncActionType ActionType => SyncActionType.NoOp;

    public void Execute(SyncContext ctx)
    {
        Console.WriteLine($"[NoOp] {ctx.LocalFilePath} – nothing to do.");
    }
}
