using Spectre.Console;
using Spectre.Console.Cli;

namespace BrandshareDamSync.Cli.Commands;

public sealed class StopCommand : Command
{
    public override int Execute(CommandContext context)
    {
        AnsiConsole.MarkupLine("[yellow]Use Ctrl+C in the terminal running 'start'.[/]");
        return 0;
    }
}
