using Spectre.Console;
using Spectre.Console.Cli;
using BrandshareDamSync.App;

namespace BrandshareDamSync.Cli.Commands;

public sealed class JobListCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var app = new AppContextContainer();
        var ct = CancellationToken.None;
        var cfg = await app.Config.LoadAsync(ct);

        if (cfg.Jobs.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No jobs.[/]");
            return 0;
        }

        var t = new Table().Border(TableBorder.Rounded).Title("Jobs");
        t.AddColumn("Id"); t.AddColumn("Name"); t.AddColumn("Dir");
        t.AddColumn("Interval"); t.AddColumn("Local"); t.AddColumn("DAM"); t.AddColumn("Enabled");

        foreach (var j in cfg.Jobs)
            t.AddRow(j.Id.ToString("N"), j.Name, j.Direction.ToString(),
                $"{j.SyncIntervalMinutes}m", j.LocalFolder, j.DamFolderId,
                j.Enabled ? "Yes" : "No");

        AnsiConsole.Write(t);
        return 0;
    }
}
