using BrandshareDamSync.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrandshareDamSync.Abstractions;

public interface ISyncAction
{
    SyncActionType ActionType { get; }
    void Execute(SyncContext ctx);
}

