using BrandshareDamSync.Domain;

namespace BrandshareDamSync.Abstractions.SyncActions;

public sealed class MoveAction : ISyncAction 
{
    public SyncActionType ActionType => SyncActionType.Move;

    public void Execute(SyncContext ctx)
    {
        Directory.CreateDirectory(ctx.ExpectedLocalDir);
        Console.WriteLine($"[Move] {ctx.LocalFilePath} -> {ctx.ExpectedLocalPath}");
        // FileHelpers.MoveFileOverwrite(ctx.LocalFilePath, ctx.ExpectedLocalPath);
    }    
}
