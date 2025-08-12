// Program.cs (Spectre bootstrap — relevant bits)
using Spectre.Console.Cli;
using BrandshareDamSync.Cli.Commands;

var app = new CommandApp();

app.Configure(cfg =>
{
    cfg.SetApplicationName("bs-dam-sync");
    cfg.AddCommand<ServiceCommand>("service")
       .WithDescription("Interactive menu (no arguments).");

    cfg.AddCommand<DaemonRunCommand>("daemon")
       .IsHidden() // used only by OS services or by "Daemon: Start (foreground)"
       .WithDescription("Run background daemon (non-interactive).");

    // Optional: make the menu the default when no command is given
    // cfg.SetDefaultCommand<ServiceCommand>();
});

return app.Run(args);
