using Spectre.Console.Cli;
using BrandshareDamSync.Cli.Commands;

public sealed class Program
{
    public static async Task<int> Main(string[] args)
    {
        var app = new CommandApp();

        app.Configure(cfg =>
        {
            cfg.SetApplicationName("brandshare-dam-sync");

            cfg.AddBranch("job", b =>
            {
                b.AddCommand<JobAddCommand>("add");
                b.AddCommand<JobListCommand>("list");
                b.AddCommand<JobRunOnceCommand>("run-once");
            });

            cfg.AddCommand<SetupCommand>("setup");
            cfg.AddCommand<StartCommand>("start");
            cfg.AddCommand<StopCommand>("stop");
            cfg.AddCommand<StatusCommand>("status");
            cfg.AddCommand<WatchCommand>("watch");
            cfg.AddCommand<SimulateCommand>("simulate");
            cfg.AddCommand<AiAskCommand>("ai");

            // NEW: interactive menu command
            cfg.AddCommand<MenuCommand>("menu").WithDescription("Interactive menu");
        });

        // Default to interactive menu when no arguments are provided
        if (args is null || args.Length == 0)
            args = new[] { "menu" };

        return await app.RunAsync(args);
    }
}
