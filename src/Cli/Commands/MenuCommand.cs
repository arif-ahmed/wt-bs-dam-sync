using Spectre.Console;
using Spectre.Console.Cli;
using BrandshareDamSync.App;
using BrandshareDamSync.Core.Models;

namespace BrandshareDamSync.Cli.Commands;

public sealed class MenuCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var app = new AppContextContainer();
        var ct = CancellationToken.None;

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Brandshare DAM Sync — choose an action[/]")
                    .PageSize(10)
                    .AddChoices(new[]
                    {
                        "Setup",
                        "Jobs: Add",
                        "Jobs: List",
                        "Jobs: Run Once",
                        "Daemon: Start (foreground)",
                        "Daemon: Status",
                        "Daemon: Watch",
                        "AI: Ask",
                        "Simulate (dry-run all jobs)",
                        "Exit"
                    }));

            switch (choice)
            {
                case "Setup":
                    await DoSetup(app, ct);
                    break;

                case "Jobs: Add":
                    await DoJobAdd(app, ct);
                    break;

                case "Jobs: List":
                    await DoJobList(app, ct);
                    break;

                case "Jobs: Run Once":
                    await DoJobRunOnce(app, ct);
                    break;

                case "Daemon: Start (foreground)":
                    await DoStartDaemon(app);
                    break;

                case "Daemon: Status":
                    await DoStatus(app, ct);
                    break;

                case "Daemon: Watch":
                    DoWatch(app);
                    break;

                case "AI: Ask":
                    await DoAiAsk(app);
                    break;

                case "Simulate (dry-run all jobs)":
                    await DoSimulate(app, ct);
                    break;

                case "Exit":
                    return 0;
            }
        }
    }

    private static async Task DoSetup(AppContextContainer app, CancellationToken ct)
    {
        var domain = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter DAM domain (e.g., https://example.brandshare.dam):")
                .Validate(s => string.IsNullOrWhiteSpace(s) ? ValidationResult.Error("Domain required") : ValidationResult.Success()));

        var apiKey = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter API key:")
                .Secret());

        var cfg = await app.Config.LoadAsync(ct);
        cfg.DamDomain = domain;
        await app.Secrets.SetAsync(cfg.ApiKeyRef, apiKey, ct);
        await app.Config.SaveAsync(cfg, ct);

        AnsiConsole.MarkupLine("[green]Setup complete.[/]");
    }

    private static async Task DoJobAdd(AppContextContainer app, CancellationToken ct)
    {
        var name = AnsiConsole.Prompt(new TextPrompt<string>("Job name:").DefaultValue("New Job"));
        var direction = AnsiConsole.Prompt(
            new SelectionPrompt<JobDirection>()
                .Title("Direction:")
                .AddChoices(Enum.GetValues<JobDirection>()));

        var local = AnsiConsole.Prompt(new TextPrompt<string>("Local folder:").DefaultValue("./"));
        var dam = AnsiConsole.Prompt(new TextPrompt<string>("DAM folder id/path:").DefaultValue("root"));
        var interval = AnsiConsole.Prompt(new TextPrompt<int>("Sync interval (minutes):").DefaultValue(5));

        var cfg = await app.Config.LoadAsync(ct);
        var job = new Job
        {
            Name = name,
            Direction = direction,
            LocalFolder = local,
            DamFolderId = dam,
            SyncIntervalMinutes = interval,
            Enabled = true
        };
        cfg.Jobs.Add(job);
        await app.Config.SaveAsync(cfg, ct);

        AnsiConsole.MarkupLine($"[green]Added job[/] {job.Name} ({job.Direction}) id [bold]{job.Id:N}[/]");
    }

    private static async Task DoJobList(AppContextContainer app, CancellationToken ct)
    {
        var cfg = await app.Config.LoadAsync(ct);
        if (cfg.Jobs.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No jobs configured.[/]");
            return;
        }

        var t = new Table().Border(TableBorder.Rounded).Title("Jobs");
        t.AddColumn("Id"); t.AddColumn("Name"); t.AddColumn("Dir");
        t.AddColumn("Interval"); t.AddColumn("Local"); t.AddColumn("DAM"); t.AddColumn("Enabled");

        foreach (var j in cfg.Jobs)
            t.AddRow(j.Id.ToString("N"), j.Name, j.Direction.ToString(),
                $"{j.SyncIntervalMinutes}m", j.LocalFolder, j.DamFolderId,
                j.Enabled ? "Yes" : "No");

        AnsiConsole.Write(t);
    }

    private static async Task DoJobRunOnce(AppContextContainer app, CancellationToken ct)
    {
        var cfg = await app.Config.LoadAsync(ct);
        if (cfg.Jobs.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No jobs available. Add one first.[/]");
            return;
        }

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<Job>()
                .Title("Select a job to run once:")
                .UseConverter(j => $"{j.Name} [{j.Direction}] ({j.Id:N})")
                .AddChoices(cfg.Jobs));

        using var runCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await app.Scheduler.RunJobOnce(pick, runCts.Token);
        app.State.JobStatuses.TryGetValue(pick.Id, out var s);
        AnsiConsole.MarkupLine($"[green]Done[/]: {s}");
    }

    private static async Task DoStartDaemon(AppContextContainer app)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        await app.Scheduler.StartAsync(cts.Token);
        AnsiConsole.MarkupLine("[green]Daemon started.[/] Press Ctrl+C to stop.");

        try { await Task.Delay(Timeout.Infinite, cts.Token); }
        catch (OperationCanceledException) { }

        await app.Scheduler.StopAsync();
        AnsiConsole.MarkupLine("[yellow]Daemon stopped.[/]");
    }

    private static async Task DoStatus(AppContextContainer app, CancellationToken ct)
    {
        var cfg = await app.Config.LoadAsync(ct);

        var table = new Table().Border(TableBorder.Rounded).Title("[bold]Brandshare DAM Sync — Status[/]");
        table.AddColumn("Daemon"); table.AddColumn("Started");
        table.AddRow(app.State.DaemonRunning ? "[green]Running[/]" : "[red]Stopped[/]",
            app.State.StartedAt == default ? "-" : app.State.StartedAt.ToString("u"));
        AnsiConsole.Write(table);

        if (cfg.Jobs.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No jobs configured.[/]");
            return;
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
    }

    private static void DoWatch(AppContextContainer app)
    {
        var interval = AnsiConsole.Prompt(new TextPrompt<int>("Refresh every N seconds:").DefaultValue(2));
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
                Thread.Sleep(TimeSpan.FromSeconds(Math.Max(1, interval)));
            }
        });
    }

    private static async Task DoAiAsk(AppContextContainer app)
    {
        var q = AnsiConsole.Prompt(new TextPrompt<string>("Ask the AI:"));
        var answer = await app.Assistant.AskAsync(q, CancellationToken.None);
        AnsiConsole.MarkupLine($"[italic]{answer}[/]");
    }

    private static async Task DoSimulate(AppContextContainer app, CancellationToken ct)
    {
        var cfg = await app.Config.LoadAsync(ct);
        if (cfg.Jobs.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No jobs. Add one first.[/]");
            return;
        }

        foreach (var job in cfg.Jobs)
        {
            AnsiConsole.MarkupLine($":gear: [bold]{job.Name}[/] ({job.Direction})");
            await app.Scheduler.RunJobOnce(job, ct);
            app.State.JobStatuses.TryGetValue(job.Id, out var s);
            AnsiConsole.MarkupLine($" → {s}");
        }
        AnsiConsole.MarkupLine("[green]Simulation complete[/]");
    }
}
