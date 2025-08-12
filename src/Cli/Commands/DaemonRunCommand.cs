// DaemonRunCommand.cs (non-interactive, hidden)
using BrandshareDamSync.Daemon;
using BrandshareDamSync.Daemon.Ipc;
using Spectre.Console.Cli;

namespace BrandshareDamSync.Cli.Commands;

public sealed class DaemonRunCommand : Command
{
    public override int Execute(CommandContext context)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        // Run the background process (blocks until Ctrl+C or service stop)
        DaemonRuntime.RunAsync(cts.Token).GetAwaiter().GetResult();
        return 0;
    }
}
