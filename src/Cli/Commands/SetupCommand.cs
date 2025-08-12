using Spectre.Console;
using Spectre.Console.Cli;
using BrandshareDamSync.App;

namespace BrandshareDamSync.Cli.Commands;

public sealed class SetupSettings : CommandSettings
{
    [CommandOption("-d|--domain <DAM_DOMAIN>")]
    public string? DamDomain { get; set; }

    [CommandOption("-k|--api-key <API_KEY>")]
    public string? ApiKey { get; set; }
}

public sealed class SetupCommand : AsyncCommand<SetupSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetupSettings settings)
    {
        var app = new AppContextContainer();
        var ct = CancellationToken.None;

        var cfg = await app.Config.LoadAsync(ct);
        if (!string.IsNullOrWhiteSpace(settings.DamDomain))
            cfg.DamDomain = settings.DamDomain;

        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            await app.Secrets.SetAsync(cfg.ApiKeyRef, settings.ApiKey, ct);

        await app.Config.SaveAsync(cfg, ct);
        AnsiConsole.MarkupLine("[green]Setup complete.[/]");
        return 0;
    }
}
