namespace BrandshareDamSync.Infrastructure.Download.Options;
public sealed class FileDownloadOptions
{
    public int MaxConcurrent { get; set; } = 5;
    public int MaxAttempts { get; set; } = 3;
    public double InitialBackoffSeconds { get; set; } = 1.0;
}