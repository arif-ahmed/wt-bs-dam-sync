// src/Daemon/Ipc/NamedPipeIpcServer.cs
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BrandshareDamSync.Daemon.Ipc;

/// <summary>
/// Listens on a user-scoped named pipe; handles one JSON line per request.
/// </summary>
public sealed class NamedPipeIpcServer : BackgroundService
{
    private readonly ILogger<NamedPipeIpcServer> _log;
    private readonly string _pipeName;

    public NamedPipeIpcServer(ILogger<NamedPipeIpcServer> log)
    {
        _log = log;
        var user = Environment.UserName?.Replace('\\', '_').Replace('/', '_');
        _pipeName = $"myapp-sync-{user}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("IPC server listening on pipe {Pipe}", _pipeName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(stoppingToken);

                using var reader = new StreamReader(server, Encoding.UTF8, leaveOpen: true);
                using var writer = new StreamWriter(server, Encoding.UTF8, bufferSize: 1024, leaveOpen: true)
                { AutoFlush = true };

                var line = await reader.ReadLineAsync();
                if (line is null) continue;

                IpcRequest? req = JsonSerializer.Deserialize<IpcRequest>(line);
                var resp = await HandleAsync(req);
                await writer.WriteLineAsync(JsonSerializer.Serialize(resp));
            }
            catch (OperationCanceledException) { /* normal on shutdown */ }
            catch (Exception ex)
            {
                _log.LogError(ex, "IPC server error; continuing");
                await Task.Delay(300, stoppingToken);
            }
        }
    }

    private Task<IpcResponse> HandleAsync(IpcRequest? req)
    {
        if (req is null) return Task.FromResult(new IpcResponse(false, "Bad request"));

        return req.Command switch
        {
            IpcCommand.Ping => Task.FromResult(new IpcResponse(true, "pong")),
            IpcCommand.GetStatus => Task.FromResult(new IpcResponse(true, "ok", "{\"state\":\"idle\"}")),
            IpcCommand.Start => Task.FromResult(new IpcResponse(true, "started")),
            IpcCommand.Stop => Task.FromResult(new IpcResponse(true, "stopped")),
            _ => Task.FromResult(new IpcResponse(false, "Unknown command"))
        };
    }
}
