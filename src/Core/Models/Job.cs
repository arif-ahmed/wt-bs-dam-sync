using System.Text.Json.Serialization;

namespace BrandshareDamSync.Core.Models;

public sealed class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Unnamed Job";
    public string DamFolderId { get; set; } = "dam-folder-id";
    public string LocalFolder { get; set; } = "./";
    public int SyncIntervalMinutes { get; set; } = 10;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public JobDirection Direction { get; set; } = JobDirection.OneWayUpload;
    public bool Enabled { get; set; } = true;
}
