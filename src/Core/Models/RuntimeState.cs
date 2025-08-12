using System.Collections.Concurrent;

namespace BrandshareDamSync.Core.Models;

public sealed class RuntimeState
{
    public bool DaemonRunning { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public ConcurrentDictionary<Guid, string> JobStatuses { get; } = new();
}
