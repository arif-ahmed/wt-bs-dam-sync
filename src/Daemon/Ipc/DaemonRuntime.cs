// DaemonRuntime.cs (shell only; no business logic)
using BrandshareDamSync.Daemon.Ipc;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace BrandshareDamSync.Daemon.Ipc;

public static class DaemonRuntime
{
    private static volatile bool _started;
    private static readonly Stopwatch _uptime = new();

    public static async Task RunAsync(CancellationToken ct = default)
    {
        _uptime.Restart();

        // Start your real worker/scheduler here later when _started == true.
        // For now we just simulate state toggling via IPC "Start"/"Stop".
        var ipcTask = RunIpcServerAsync(ct);

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException) { /* normal shutdown */ }

        await ipcTask;
    }

    private static async Task RunIpcServerAsync(CancellationToken ct)
    {
        var user = Environment.UserName?.Replace('\\', '_').Replace('/', '_');
        var pipeName = $"bs-dam-sync-{user}";

        // Re-accept clients forever (one-at-a-time is fine for a control surface)
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(ct);

                using var reader = new StreamReader(server, Encoding.UTF8, leaveOpen: true);
                using var writer = new StreamWriter(server, Encoding.UTF8, 1024, leaveOpen: true) { AutoFlush = true };

                var line = await reader.ReadLineAsync();
                if (line is null) continue;

                IpcRequest? req = null;
                try { req = JsonSerializer.Deserialize<IpcRequest>(line); }
                catch (Exception ex)
                {
                    await writer.WriteLineAsync(JsonSerializer.Serialize(new IpcResponse(false, $"Bad request: {ex.Message}")));
                    continue;
                }

                var resp = Handle(req!);
                await writer.WriteLineAsync(JsonSerializer.Serialize(resp));
            }
            catch (OperationCanceledException) { /* stopping */ }
            catch
            {
                // Keep the daemon resilient; brief backoff on transient errors.
                await Task.Delay(250, ct);
            }
        }
    }

    private static IpcResponse Handle(IpcRequest req)
    {
        switch (req.Command)
        {
            case IpcCommand.Ping:
                return new IpcResponse(true, "pong");

            case IpcCommand.Start:
                _started = true;
                return new IpcResponse(true, "started");

            case IpcCommand.Stop:
                _started = false;
                return new IpcResponse(true, "stopped");

            case IpcCommand.GetStatus:
                var payload = new
                {
                    started = _started,
                    uptime_ms = _uptime.ElapsedMilliseconds,
                    state = _started ? "running" : "idle"
                };
                return new IpcResponse(true, "ok", JsonSerializer.Serialize(payload));

            default:
                return new IpcResponse(false, "unknown command");
        }
    }
}
