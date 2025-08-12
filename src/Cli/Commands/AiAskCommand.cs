using Spectre.Console;
using Spectre.Console.Cli;
using BrandshareDamSync.App;

namespace BrandshareDamSync.Cli.Commands;

public sealed class AiAskSettings : CommandSettings
{
    [CommandArgument(0, "<QUESTION>")]
    public string Question { get; set; } = "";
}

public sealed class AiAskCommand : AsyncCommand<AiAskSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AiAskSettings settings)
    {
        var app = new AppContextContainer();
        var ct = CancellationToken.None;

        var answer = await app.Assistant.AskAsync(settings.Question, ct);
        AnsiConsole.MarkupLine($"[italic]{answer}[/]");
        return 0;
    }
}
