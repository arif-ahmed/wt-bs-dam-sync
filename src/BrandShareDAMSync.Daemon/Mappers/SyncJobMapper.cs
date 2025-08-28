
using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure.BrandShareDamClient;

namespace BrandshareDamSync.Daemon.Mappers
{
    public static class SyncJobMapper
    {
        public static SyncJob ToEntity(ApiSyncJob job, string tenantId)
        {
            var nowUtc = DateTime.UtcNow;
            return new SyncJob
            {
                Id = job.Id,
                TenantId = tenantId,
                JobName = job.JobName ?? string.Empty,
                VolumeName = job.VolumeName ?? string.Empty,
                VolumePath = job.VolumePath ?? string.Empty,
                VolumeId = job.VolumeId ?? string.Empty,
                SyncDirection = ParseSyncDirection(job.SyncDirection),
                DestinationPath = NormalisePath(job.DestinationPath),
                JobIntervalMinutes = job.JobInterval,
                JobStatus = job.JobStatus ?? string.Empty,
                SyncJobStatus = MapStatus(job.JobStatus),
                IsActive = job.IsActive,
                PrimaryLocation = job.PrimaryLocation ?? string.Empty,
                CreatedAtUtc = nowUtc,
                LastModifiedAtUtc = nowUtc
            };
        }

        private static SyncDirection ParseSyncDirection(string? apiValue) =>
            apiValue?.Trim().ToUpperInvariant() switch
            {
                "D2L" => SyncDirection.D2L,
                "L2D" => SyncDirection.L2D,
                "D2LD" => SyncDirection.D2LD,
                "L2DD" => SyncDirection.L2DD,
                "BOTH" => SyncDirection.Both,
                _ => SyncDirection.Unknown
            };

        private static SyncJobStatus MapStatus(string? apiJobStatus)
        {
            var s = (apiJobStatus ?? "").Trim().ToLowerInvariant();
            if (s.Contains("start")) return SyncJobStatus.Running;
            if (s.Contains("run")) return SyncJobStatus.Running;
            // if (s.Contains("pause")) return SyncJobStatus.Paused;
            if (s.Contains("stop")) return SyncJobStatus.Cancelled;
            if (s.Contains("error") || s.Contains("fail")) return SyncJobStatus.Failed;
            return SyncJobStatus.Unknown;
        }

        private static string NormalisePath(string? path) =>
            string.IsNullOrWhiteSpace(path) ? string.Empty : path.Replace("\r", "").Replace("\n", "").Trim();
    }
}
