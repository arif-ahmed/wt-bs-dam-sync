using Spectre.Console;
using Spectre.Console.Cli;
using BrandshareDamSync.App;

namespace BrandshareDamSync.Cli.Commands;

public sealed class JobRunOnceSettings : CommandSettings
{
    [CommandOption("--id <JOB_ID>")]
    public string Id { get; set; } = "";
}

public sealed class JobRunOnceCommand : AsyncCommand<JobRunOnceSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, JobRunOnceSettings settings)
    {
        var app = new AppContextContainer();
        var ct = CancellationToken.None;

        var cfg = await app.Config.LoadAsync(ct);
        var job = cfg.Jobs.FirstOrDefault(j => j.Id.ToString("N").Equals(settings.Id, StringComparison.OrdinalIgnoreCase));
        if (job is null)
        {
            AnsiConsole.MarkupLine("[red]Job not found[/]");
            return 1;
        }

        using var runCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await app.Scheduler.RunJobOnce(job, runCts.Token);

        app.State.JobStatuses.TryGetValue(job.Id, out var s);
        AnsiConsole.MarkupLine($"[green]Done[/]: {s}");
        return 0;
    }
}
