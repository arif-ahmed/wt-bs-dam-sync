using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using BrandshareDamSync.Cli.Commands;
using BrandshareDamSync.Infrastructure.Secrets;

public sealed class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Ensure key is in the secret store
        await EnsureAzureOpenAiKeyFromEnvOrConfigAsync();

        var app = new CommandApp();
        app.Configure(cfg =>
        {
            cfg.SetApplicationName("brandshare-dam-sync");

            cfg.AddBranch("job", b =>
            {
                b.AddCommand<JobAddCommand>("add").WithDescription("Add a new job");
                b.AddCommand<JobListCommand>("list").WithDescription("List configured jobs");
                b.AddCommand<JobRunOnceCommand>("run-once").WithDescription("Run a job once immediately");
            });

            cfg.AddCommand<SetupCommand>("setup").WithDescription("Initialise config and store API key");
            cfg.AddCommand<StartCommand>("start").WithDescription("Start daemon (foreground; Ctrl+C to stop)");
            cfg.AddCommand<StopCommand>("stop").WithDescription("Stop daemon (Ctrl+C in this build)");
            cfg.AddCommand<StatusCommand>("status").WithDescription("Show daemon and job status");
            cfg.AddCommand<WatchCommand>("watch").WithDescription("Continuously show job status");
            cfg.AddCommand<SimulateCommand>("simulate").WithDescription("Dry-run all jobs once");
            cfg.AddCommand<AiAskCommand>("ai").WithDescription("Ask the AI assistant a question");

            cfg.AddCommand<MenuCommand>("menu").WithDescription("Interactive menu");
        });

        if (args is null || args.Length == 0)
            args = new[] { "menu" };

        try
        {
            return await app.RunAsync(args);
        }
        catch (CommandParseException ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks);
            return -1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]An unexpected error occurred.[/]");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks);
            return -1;
        }
    }

    private static async Task EnsureAzureOpenAiKeyFromEnvOrConfigAsync()
    {
        var secrets = new SimpleSecretStore();
        var existingKey = await secrets.GetAsync("azureopenai:key", CancellationToken.None);

        if (string.IsNullOrWhiteSpace(existingKey))
        {
            // 1) Try environment variable first
            var envKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                await secrets.SetAsync("azureopenai:key", envKey, CancellationToken.None);
                AnsiConsole.MarkupLine("[green]Azure OpenAI API key loaded from environment and stored securely.[/]");
                return;
            }

            // 2) Try appsettings.json
            var keyFromFile = LoadApiKeyFromAppSettings();
            if (!string.IsNullOrWhiteSpace(keyFromFile))
            {
                await secrets.SetAsync("azureopenai:key", keyFromFile, CancellationToken.None);
                AnsiConsole.MarkupLine("[green]Azure OpenAI API key loaded from appsettings.json and stored securely.[/]");
                return;
            }

            // 3) Fallback warning
            AnsiConsole.MarkupLine("[red]Azure OpenAI API key not found in environment or appsettings.json.[/]");
        }
    }

    private static string? LoadApiKeyFromAppSettings()
    {
        // Look for appsettings.json in both BaseDirectory and current working directory
        var candidates = new[]
        {
        Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")
    };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
                continue;

            try
            {
                using var fs = File.OpenRead(path);
                using var doc = JsonDocument.Parse(fs);
                if (doc.RootElement.TryGetProperty("AzureOpenAI", out var aoai) &&
                    aoai.TryGetProperty("ApiKey", out var apiKeyElement))
                {
                    var key = apiKeyElement.GetString();
                    if (!string.IsNullOrWhiteSpace(key))
                        return key.Trim();
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Failed to read API key from {path}: {ex.Message}[/]");
            }
        }

        return null;
    }

}
