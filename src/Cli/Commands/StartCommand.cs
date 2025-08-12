using Spectre.Console;
using Spectre.Console.Cli;
using BrandshareDamSync.App;

namespace BrandshareDamSync.Cli.Commands;

public sealed class StartCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var app = new AppContextContainer();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        await app.Scheduler.StartAsync(cts.Token);
        AnsiConsole.MarkupLine("[green]Daemon started.[/] Press Ctrl+C to stop.");

        try { await Task.Delay(Timeout.Infinite, cts.Token); }
        catch (OperationCanceledException) { }

        await app.Scheduler.StopAsync();
        AnsiConsole.MarkupLine("[yellow]Daemon stopped.[/]");
        return 0;
    }
}
