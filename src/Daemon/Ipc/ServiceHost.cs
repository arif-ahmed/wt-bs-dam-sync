// src/Daemon/ServiceHost.cs
using BrandshareDamSync.Daemon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace BrandshareDamSync.Daemon.Ipc;

/// <summary>
/// Builds the .NET Generic Host, wires logging, Worker, and IPC.
/// </summary>
public static class ServiceHost
{
    public static async Task<int> RunAsync(string[] args, CancellationToken externalToken = default)
    {
        using var host = Host.CreateDefaultBuilder(args)
#if WINDOWS
            .UseWindowsService()  // requires Microsoft.Extensions.Hosting.WindowsServices
#endif
#if LINUX
            .UseSystemd()         // requires Microsoft.Extensions.Hosting.Systemd
#endif
            .ConfigureLogging(lb =>
            {
                lb.ClearProviders();
                lb.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });
            })
            .ConfigureServices(svc =>
            {
                // Register your own singletons here later (config, scheduler, etc.)
                svc.AddHostedService<SyncWorker>();
                svc.AddHostedService<NamedPipeIpcServer>();
            })
            .Build();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        await host.StartAsync(cts.Token);
        try { await host.WaitForShutdownAsync(cts.Token); }
        catch (OperationCanceledException) { /* normal */ }

        return 0;
    }
}
