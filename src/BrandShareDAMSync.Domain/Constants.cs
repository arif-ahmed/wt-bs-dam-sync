namespace BrandshareDamSync.Domain;

public class SyncJobType
{
    public const string DOWNLOAD = "download";
    public const string DOWNLOAD_AND_CLEAN = "download_and_clean";
    public const string UPLOAD = "upload";
    public const string UPLOAD_AND_CLEAN = "download_and_clean";
    public const string BOTH = "both";
}
