// src/Cli/Commands/ServiceCommand.cs
using Spectre.Console;
using Spectre.Console.Cli;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

using BrandshareDamSync.Daemon.Ipc;


namespace BrandshareDamSync.Cli.Commands;

public sealed class ServiceCommand : Command
{
    public override int Execute(CommandContext context)
    {
        while (true)
        {
            AnsiConsole.MarkupLine("[bold]Brandshare DAM Sync - choose an action[/]");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .PageSize(12)
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
                    Info("setup wizard");
                    break;

                case "Jobs: Add":
                    Info("prompt for job details → save config");
                    break;

                case "Jobs: List":
                    Info("read config → list jobs");
                    break;

                case "Jobs: Run Once":
                    Info("pick a job → send IPC to run once");
                    break;

                case "Daemon: Start (foreground)":
                    RunDaemonForeground();
                    break;

                case "Daemon: Status":
                    SendIpc(new IpcRequest(IpcCommand.GetStatus));
                    break;

                case "Daemon: Watch":
                    WatchStatus();
                    break;

                case "AI: Ask":
                    Info("ask AI helper");
                    break;

                case "Simulate (dry-run all jobs)":
                    Info("simulate all jobs and summarise");
                    break;

                case "Exit":
                    AnsiConsole.MarkupLine("[grey]Goodbye.[/]");
                    return 0;
            }

            Pause();
        }
    }

    // ---- helpers (shell only) ----

    private static void RunDaemonForeground()
    {
        AnsiConsole.MarkupLine("[grey]Starting daemon in foreground. Press Ctrl+C to stop...[/]");
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        DaemonRuntime.RunAsync(cts.Token).GetAwaiter().GetResult();
    }

    private static void WatchStatus()
    {
        AnsiConsole.MarkupLine("[grey]Watching status. Press Ctrl+C to stop...[/]");
        try
        {
            while (true)
            {
                var ok = SendIpc(new IpcRequest(IpcCommand.GetStatus), quiet: true);
                if (!ok) AnsiConsole.MarkupLine("[red]service offline[/]");
                Thread.Sleep(1000);
            }
        }
        catch (Exception) { /* interrupted by Ctrl+C */ }
    }

    private static bool SendIpc(IpcRequest request, bool quiet = false)
    {
        try
        {
            var user = Environment.UserName?.Replace('\\', '_').Replace('/', '_');
            var pipe = $"bs-dam-sync-{user}";

            using var client = new NamedPipeClientStream(".", pipe, PipeDirection.InOut);
            client.Connect(800);

            using var writer = new StreamWriter(client, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(client, Encoding.UTF8, leaveOpen: true);

            writer.WriteLine(JsonSerializer.Serialize(request));
            var line = reader.ReadLine();
            if (line is null) { if (!quiet) AnsiConsole.MarkupLine("[red]No response from daemon[/]"); return false; }

            var resp = JsonSerializer.Deserialize<IpcResponse>(line!);
            if (resp is null) { if (!quiet) AnsiConsole.MarkupLine("[red]Malformed response[/]"); return false; }

            if (!quiet)
            {
                AnsiConsole.MarkupLine(resp.Ok ? $"[green]{resp.Message}[/]" : $"[red]{resp.Message}[/]");
                if (!string.IsNullOrWhiteSpace(resp.PayloadJson))
                    AnsiConsole.WriteLine(resp.PayloadJson);
            }

            return resp.Ok;
        }
        catch (Exception ex)
        {
            if (!quiet) AnsiConsole.MarkupLine($"[red]IPC error:[/] {ex.Message}");
            return false;
        }
    }

    private static void Info(string msg) => AnsiConsole.MarkupLine($"[grey]TODO: {msg}[/]");
    private static void Pause() { AnsiConsole.MarkupLine("[grey](Press Enter to return to the menu)[/]"); Console.ReadLine(); }
}
