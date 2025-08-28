using BrandshareDamSync.Abstractions;
using Microsoft.Extensions.Logging;

namespace BrandshareDamSync.Infrastructure.JobExecutors;

public class UploadAndCleanJobExecutor(ILogger<UploadAndCleanJobExecutor> logger) : IJobExecutorService
{
    public async Task ExecuteJobAsync((string syncId, string tenantId, string jobId) syncJobInfo, CancellationToken ct = default)
    {
        (string syncId, string tenantId, string jobId) = syncJobInfo;

        logger.LogInformation($"Job: {jobId} Started");
        await Task.Delay(1 * 60 * 1000, ct);
        logger.LogInformation($"Job: {jobId} finished");
    }
}
