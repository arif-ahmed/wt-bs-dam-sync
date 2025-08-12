using Spectre.Console;
using Spectre.Console.Cli;
using BrandshareDamSync.App;

namespace BrandshareDamSync.Cli.Commands;

public sealed class StatusCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var app = new AppContextContainer();
        var ct = CancellationToken.None;
        var cfg = await app.Config.LoadAsync(ct);

        var table = new Table().Border(TableBorder.Rounded).Title("[bold]Brandshare DAM Sync — Status[/]");
        table.AddColumn("Daemon");
        table.AddColumn("Started");
        table.AddRow(app.State.DaemonRunning ? "[green]Running[/]" : "[red]Stopped[/]",
            app.State.StartedAt == default ? "-" : app.State.StartedAt.ToString("u"));
        AnsiConsole.Write(table);

        if (cfg.Jobs.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No jobs configured.[/]");
            return 0;
        }

        var jt = new Table().Border(TableBorder.Rounded).Title("Jobs");
        jt.AddColumn("Id"); jt.AddColumn("Name"); jt.AddColumn("Dir");
        jt.AddColumn("Interval"); jt.AddColumn("Local"); jt.AddColumn("DAM");
        jt.AddColumn("Enabled"); jt.AddColumn("Last Status");

        foreach (var j in cfg.Jobs)
        {
            app.State.JobStatuses.TryGetValue(j.Id, out var s);
            jt.AddRow(j.Id.ToString("N"), j.Name, j.Direction.ToString(),
                $"{j.SyncIntervalMinutes}m", j.LocalFolder, j.DamFolderId,
                j.Enabled ? "Yes" : "No", s ?? "-");
        }
        AnsiConsole.Write(jt);
        return 0;
    }
}
