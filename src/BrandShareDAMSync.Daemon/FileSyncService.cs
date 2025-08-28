using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Abstractions.SyncActions;
using BrandshareDamSync.Daemon.Common;
using BrandshareDamSync.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrandshareDamSync.Daemon;

public sealed class FileSyncService
{
    // Simple action registry; could be DI-injected if preferred
    private readonly ISyncAction[] _actions =
    {
        new RedownloadAction(),
        new RenameAction(),
        new MoveAction(),
        new NoOpAction()
    };

    public void Sync(SyncContext ctx)
    {
        var decision = SyncDecisionFactory.Decide(ctx);
        var action = GetAction(decision.Action);
        Console.WriteLine($"Decision: {decision.Action} — {decision.Reason}");
        action.Execute(ctx);
    }

    private ISyncAction GetAction(SyncActionType type)
    {
        foreach (var act in _actions)
            if (act.ActionType == type) return act;

        // Fallback
        return new NoOpAction();
    }
}
