using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrandshareDamSync.Daemon;

/// <summary>
/// Long-running background loop. No real logic here; delegate to your scheduler later.
/// </summary>
public sealed class SyncWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // TODO: start your scheduler here (e.g., _scheduler.StartAsync(stoppingToken))
        try
        {
            // Keep process alive until stopped by the host/service manager.
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Swallow expected cancellation when service stops.
        }
        finally
        {
            // TODO: gracefully stop your scheduler, flush, dispose, etc.
        }
    }
}
