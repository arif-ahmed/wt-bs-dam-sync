using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using BrandshareDamSync.App;

namespace BrandshareDamSync.Cli.Commands;

public sealed class WatchSettings : CommandSettings
{
    [CommandOption("-i|--interval <SECONDS>")]
    [DefaultValue(2)]
    public int IntervalSeconds { get; set; } = 2;
}

public sealed class WatchCommand : AsyncCommand<WatchSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, WatchSettings settings)
    {
        var app = new AppContextContainer();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        AnsiConsole.Live(new Table().AddColumn("Job").AddColumn("Status")).Start(ctx =>
        {
            while (!cts.IsCancellationRequested)
            {
                var t = new Table().AddColumn("Job").AddColumn("Status");
                foreach (var kv in app.State.JobStatuses)
                    t.AddRow(kv.Key.ToString("N"), kv.Value);
                ctx.UpdateTarget(t);
                Thread.Sleep(TimeSpan.FromSeconds(Math.Max(1, settings.IntervalSeconds)));
            }
        });
        return Task.FromResult(0);
    }
}
