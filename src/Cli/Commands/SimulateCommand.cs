using Spectre.Console;
using Spectre.Console.Cli;
using BrandshareDamSync.App;

namespace BrandshareDamSync.Cli.Commands;

public sealed class SimulateCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var app = new AppContextContainer();
        var ct = CancellationToken.None;
        var cfg = await app.Config.LoadAsync(ct);

        if (cfg.Jobs.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No jobs. Use 'job add'.[/]");
            return 0;
        }

        foreach (var job in cfg.Jobs)
        {
            AnsiConsole.MarkupLine($":gear: [bold]{job.Name}[/] ({job.Direction})");
            await app.Scheduler.RunJobOnce(job, ct);
            app.State.JobStatuses.TryGetValue(job.Id, out var s);
            AnsiConsole.MarkupLine($" ? {s}");
        }

        AnsiConsole.MarkupLine("[green]Simulation complete[/]");
        return 0;
    }
}
