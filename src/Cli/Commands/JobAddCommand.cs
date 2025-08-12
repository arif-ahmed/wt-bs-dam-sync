using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using BrandshareDamSync.App;
using BrandshareDamSync.Core.Models;

namespace BrandshareDamSync.Cli.Commands;

public sealed class JobAddSettings : CommandSettings
{
    [CommandOption("--name <NAME>")] public string Name { get; set; } = "New Job";
    [CommandOption("--dir <DIRECTION>")] public JobDirection Direction { get; set; } = JobDirection.OneWayUpload;
    [CommandOption("--local <PATH>")] public string Local { get; set; } = "./";
    [CommandOption("--dam <FOLDER>")] public string DamFolder { get; set; } = "root";
    [CommandOption("--interval <MINUTES>")][DefaultValue(5)] public int Interval { get; set; } = 5;
}

public sealed class JobAddCommand : AsyncCommand<JobAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, JobAddSettings settings)
    {
        var app = new AppContextContainer();
        var ct = CancellationToken.None;

        var cfg = await app.Config.LoadAsync(ct);
        var job = new Job
        {
            Name = settings.Name,
            Direction = settings.Direction,
            LocalFolder = settings.Local,
            DamFolderId = settings.DamFolder,
            SyncIntervalMinutes = settings.Interval,
            Enabled = true
        };

        cfg.Jobs.Add(job);
        await app.Config.SaveAsync(cfg, ct);

        AnsiConsole.MarkupLine($"[green]Added job[/] {job.Name} ({job.Direction}) id [bold]{job.Id:N}[/]");
        return 0;
    }
}
