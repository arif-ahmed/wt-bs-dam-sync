using System.Collections.Concurrent;
using BrandshareDamSync.Core.Models;
using BrandshareDamSync.Core.Strategies;
using BrandshareDamSync.Infrastructure.Config;
using BrandshareDamSync.Infrastructure.Dam;

namespace BrandshareDamSync.Core.Scheduler;

public sealed class Scheduler : IJobScheduler
{
    private readonly IConfigStore _config;
    private readonly IDamClient _dam;
    private readonly RuntimeState _state;
    private readonly Dictionary<JobDirection, IJobStrategy> _strategies;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobLoops = new();
    private CancellationTokenSource? _cts;

    public bool IsRunning => _state.DaemonRunning;

    public Scheduler(IConfigStore config, IDamClient dam, RuntimeState state, IEnumerable<IJobStrategy> strategies)
    {
        _config = config; _dam = dam; _state = state;
        _strategies = strategies.ToDictionary(s => s.Direction, s => s);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (_state.DaemonRunning) return;
        _state.DaemonRunning = true; _state.StartedAt = DateTimeOffset.Now;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var cfg = await _config.LoadAsync(ct);
        foreach (var job in cfg.Jobs.Where(j => j.Enabled)) StartJobLoop(job, _cts.Token);
        _ = Task.Run(() => PollLoopAsync(_cts.Token));
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        foreach (var kv in _jobLoops) kv.Value.Cancel();
        _state.DaemonRunning = false;
        return Task.CompletedTask;
    }

    public async Task RunJobOnce(Job job, CancellationToken ct)
    {
        if (!_strategies.TryGetValue(job.Direction, out var strategy))
            throw new InvalidOperationException($"No strategy for {job.Direction}");
        _state.JobStatuses[job.Id] = "Running...";
        await strategy.ExecuteAsync(job, _dam, _state, ct);
    }

    private void StartJobLoop(Job job, CancellationToken ct)
    {
        var inner = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _jobLoops[job.Id] = inner;
        _ = Task.Run(async () =>
        {
            while (!inner.IsCancellationRequested)
            {
                try { await RunJobOnce(job, inner.Token); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { _state.JobStatuses[job.Id] = $"Error: {ex.Message}"; }
                try { await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, job.SyncIntervalMinutes)), inner.Token); }
                catch (OperationCanceledException) { }
            }
        }, inner.Token);
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var cfg = await _config.LoadAsync(ct);
                var polled = await _dam.PollNewJobsAsync(cfg, ct);
                if (polled.Count > 0)
                {
                    cfg.Jobs.AddRange(polled);
                    await _config.SaveAsync(cfg, ct);
                    foreach (var j in polled.Where(x => x.Enabled)) StartJobLoop(j, ct);
                }
            }
            catch { }
            try
            {
                var cfg = await _config.LoadAsync(ct);
                await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, cfg.PollIntervalMinutes)), ct);
            }
            catch (OperationCanceledException) { }
        }
    }
}
