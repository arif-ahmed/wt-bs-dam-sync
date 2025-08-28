
using Amazon.S3;

namespace BrandshareDamSync.Infrastructure.S3;

public static class UploadRunner
{
    public static async Task RunAsync(
        string token,
        string bucketName,
        string localFilePath,
        string s3Key // relative key under baseDirectory; uploader will prepend it
    )
    {
        var parsed = TokenParser.Parse(token);
        var region = parsed.Region;

        // Build S3 client with creds from token
        var s3 = new AmazonS3Client(parsed.AccessKeyId, parsed.SecretKey, region);

        // baseDirectory comes from token.project
        var uploader = new S3FileUploader(s3, bucketName, parsed.BaseDirectory);

        uploader.Progress += msg => Console.WriteLine(msg);

        await uploader.UploadAsync(localFilePath, s3Key);

        Console.WriteLine("Upload completed.");
    }
}
