using System.Text.Json;
using BrandshareDamSync.Core.Models;

namespace BrandshareDamSync.Infrastructure.Config;

public sealed class JsonConfigStore : IConfigStore
{
    private readonly string _path;
    public JsonConfigStore(string? path = null)
        => _path = path ?? Path.Combine(AppContext.BaseDirectory, "config.json");

    public async Task<MachineConfig> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(_path)) return new MachineConfig();
        await using var fs = File.OpenRead(_path);
        var cfg = await JsonSerializer.DeserializeAsync<MachineConfig>(fs, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true }, ct);
        return cfg ?? new MachineConfig();
    }

    public async Task SaveAsync(MachineConfig config, CancellationToken ct)
    {
        await using var fs = File.Create(_path);
        await JsonSerializer.SerializeAsync(fs, config, new JsonSerializerOptions { WriteIndented = true }, ct);
    }
}
