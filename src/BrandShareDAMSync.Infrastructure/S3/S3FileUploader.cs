using Amazon.S3;
using Amazon.S3.Transfer;
using BrandshareDamSync.Infrastructure.BrandShareDamClient;

namespace BrandshareDamSync.Infrastructure.S3;

public sealed class S3FileUploader
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;
    private readonly string _baseDirectory;

    // Optional event for progress messages (similar to your emitter)
    public event Action<string>? Progress;

    public S3FileUploader(IAmazonS3 s3, string bucketName, string baseDirectory)
    {
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
        _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
        _baseDirectory = baseDirectory ?? string.Empty;
    }

    /// <summary>
    /// Uploads a file to S3 with progress reporting.
    /// Mirrors the Java method's behaviour and return type.
    /// </summary>
    public async Task<bool> UploadAsync(string filePath, string fileKey,
                                        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Required", nameof(filePath));
        if (string.IsNullOrWhiteSpace(fileKey)) throw new ArgumentException("Required", nameof(fileKey));

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists) throw new FileNotFoundException("File not found", filePath);

        // baseDirectory + "/" + fileKey (avoids double slashes)
        var s3Key = string.IsNullOrEmpty(_baseDirectory)
            ? fileKey
            : $"{_baseDirectory.TrimEnd('/')}/{fileKey.TrimStart('/')}";

        // Config similar to multipart thresholds in Java TransferManager
        var tuConfig = new TransferUtilityConfig
        {
            // Optional: tune concurrency if desired
            ConcurrentServiceRequests = 4
        };

        var transferUtility = new TransferUtility(_s3, tuConfig);

        // Request with multipart part size (optional, but common)
        var request = new TransferUtilityUploadRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            FilePath = filePath,

            // 8 MiB parts are a sensible default; adjust as needed
            PartSize = 8 * 1024 * 1024
        };

        int lastPct = -1;
        request.UploadProgressEvent += (sender, e) =>
        {
            // e.PercentDone is 0–100; avoid spamming duplicates like the Java example
            if (e.PercentDone > lastPct)
            {
                lastPct = e.PercentDone;
                Progress?.Invoke(
                    $"[S3FileUploader] '{fileInfo.Name}' {e.PercentDone}% " +
                    $"({e.TransferredBytes} bytes of {e.TotalBytes} bytes) Transferred");
            }
        };

        // UploadAsync blocks until complete (equivalent to waitForCompletion)
        await transferUtility.UploadAsync(request, cancellationToken).ConfigureAwait(false);

        // No explicit shutdown needed; TransferUtility cleans up its own resources
        return true;
    }

    // If you need a synchronous facade like the Java method's signature:
    public bool Upload(string filePath, string fileKey)
        => UploadAsync(filePath, fileKey).GetAwaiter().GetResult();
}
