namespace BrandshareDamSync.Core.Models;

public sealed class MachineConfig
{
    public string MachineName { get; set; } = Environment.MachineName;
    public string DamDomain { get; set; } = "https://example.brandshare.dam";
    public string ApiKeyRef { get; set; } = "brandshare:apiKey";
    public int PollIntervalMinutes { get; set; } = 10;
    public List<Job> Jobs { get; set; } = new();
}
