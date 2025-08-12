using BrandshareDamSync.Core.Models;

namespace BrandshareDamSync.Core.Scheduler;

public interface IJobScheduler
{
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    bool IsRunning { get; }
    Task RunJobOnce(Job job, CancellationToken ct);
}
