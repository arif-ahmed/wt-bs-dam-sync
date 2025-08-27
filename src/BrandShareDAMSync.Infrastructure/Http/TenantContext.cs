using System;
using System.Threading;

namespace BrandshareDamSync.Daemon.Infrastructure.Http;

public sealed class TenantContext : ITenantContext
{
    private sealed record State(string TenantId, string ApiKey, string BaseUrl);
    private static readonly AsyncLocal<State?> _state = new();

    public string? TenantId => _state.Value?.TenantId;
    public string? ApiKey => _state.Value?.ApiKey;
    public string? BaseUrl => _state.Value?.BaseUrl;

    public IDisposable Use(string tenantId, string apiKey, string baseUrl)
    {
        var prev = _state.Value;
        _state.Value = new State(tenantId, apiKey, baseUrl);
        return new Popper(() => _state.Value = prev);
    }

    private sealed class Popper : IDisposable
    {
        private readonly Action _pop;
        public Popper(Action pop) => _pop = pop;
        public void Dispose() => _pop();
    }
}
