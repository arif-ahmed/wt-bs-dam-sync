using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Daemon.Common;
using BrandshareDamSync.Domain;
using Microsoft.EntityFrameworkCore;

namespace BrandshareDamSync.Daemon;

public sealed record JobListItemDto(string Id, string? JobName, string jobType, string TenantId, string TenantName);

public class MachinePoller(ILogger<MachinePoller> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("MachinePoller started at: {time}", DateTimeOffset.Now);

            var syncId = Guid.NewGuid().ToString("N");

            using var scope = scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();


            var (items, total) = await uow.JobRepository.GetAsync<JobListItemDto>(
                predicate: q => q.IsActive,
                include: q => q.Include(j => j.Tenant),
                orderBy: q => q.OrderBy(j => j.Id),
                selector: j => new JobListItemDto(j.Id, j.JobName, Get(j.SyncDirection), j.TenantId, j.Tenant.Domain),
                cancellationToken: stoppingToken);

            const int maxConcurrency = 4;

            // Limit concurrency with a semaphore
            using var semaphore = new SemaphoreSlim(maxConcurrency);

            var jobExecutorFactory = scope.ServiceProvider.GetRequiredService<JobExecutorFactory>();

            var tasks = new List<Task>();

            foreach (var item in items)
            {
                tasks.Add(RunOneAsync(item, stoppingToken));
            }

            await WhenAllSwallowingAndLoggingAsync(tasks, logger, stoppingToken);

            // throttle the polling loop
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            // ---- local funcs ----

            async Task RunOneAsync(JobListItemDto item, CancellationToken ct)
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    // NEW SCOPE PER JOB (so scoped services are thread-safe)
                    using var jobScope = scopeFactory.CreateScope();
                    var jobExecutorFactory = jobScope.ServiceProvider.GetRequiredService<JobExecutorFactory>();
                    var jobExecutorService = jobExecutorFactory.CreateJobExecutorService(item.jobType);

                    await jobExecutorService.ExecuteJobAsync((syncId, item.TenantId, item.Id), ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // graceful cancellation
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Job {JobId} failed.", item.Id);
                    // decide: swallow to keep others going OR rethrow to fail the batch
                }
                finally
                {
                    semaphore.Release();
                }
            }

            static async Task WhenAllSwallowingAndLoggingAsync(IEnumerable<Task> tasks, ILogger logger, CancellationToken ct)
            {
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    // Task.WhenAll flattens into an AggregateException; the above per-task catch already logged details.
                    logger.LogWarning(ex, "One or more jobs failed.");
                }
            }

        }
    }

    static string Get(SyncDirection syncDirection) => syncDirection switch
    {
        SyncDirection.D2L => SyncJobType.DOWNLOAD,
        SyncDirection.L2D => SyncJobType.UPLOAD,
        SyncDirection.Both => SyncJobType.BOTH,
        _ => ""
    };
}
