//using System.Diagnostics;
//using System.Runtime.InteropServices;
//using System.Text;
//using Microsoft.Extensions.Hosting;

//namespace BrandshareDamSync.Cli;

//public static class ServiceManager
//{
//    // Names & paths
//    public const string ServiceName = "DamSync";
//    public const string ServiceUser = "root"; // adjust on Linux if you want a dedicated user
//    public static string InferWorkerPath()
//    {
//        // Try to find dam-syncd next to CLI (common after publishing)
//        var cliDir = AppContext.BaseDirectory;
//        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dam-syncd.exe" : "dam-syncd";
//        var candidate = Path.Combine(cliDir, exeName);
//        return File.Exists(candidate) ? candidate : candidate; // default anyway
//    }

//    public static Task InstallAsync(string binPath)
//        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? InstallWindowsAsync(binPath)
//         : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? InstallLaunchdAsync(binPath)
//         : InstallSystemdAsync(binPath);

//    public static Task StartAsync()
//        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? RunAsync("sc", "start " + ServiceName)
//         : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? RunAsync("launchctl", $"kickstart -k gui/{GetLoginUid()}/com.example.dam-sync")
//         : RunAsync("systemctl", $"start {ServiceName.ToLowerInvariant()}");

//    public static Task StopAsync()
//        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? RunAsync("sc", "stop " + ServiceName)
//         : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? RunAsync("launchctl", $"bootout gui/{GetLoginUid()}/com.example.dam-sync")
//         : RunAsync("systemctl", $"stop {ServiceName.ToLowerInvariant()}");

//    public static Task RemoveAsync()
//        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? RemoveWindowsAsync()
//         : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? RemoveLaunchdAsync()
//         : RemoveSystemdAsync();

//    public static Task StatusAsync()
//        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? RunAsync("sc", "query " + ServiceName)
//         : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? RunAsync("launchctl", $"print gui/{GetLoginUid()}/com.example.dam-sync")
//         : RunAsync("systemctl", $"status {ServiceName.ToLowerInvariant()} --no-pager");

//    public static async Task WatchAsync()
//    {
//        var path = DefaultLogPath();
//        Console.WriteLine($"Watching log: {path}");
//        Console.WriteLine("Press 'Q' to return to menu.\n");

//        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
//        using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
//        using var reader = new StreamReader(fs, Encoding.UTF8);

//        _ = await reader.ReadToEndAsync(); // jump to end

//        while (true)
//        {
//            // Check if user pressed Q
//            if (Console.KeyAvailable)
//            {
//                var key = Console.ReadKey(intercept: true);
//                if (key.Key == ConsoleKey.Q)
//                {
//                    Console.WriteLine("\nReturning to menu...");
//                    break; // exit loop
//                }
//            }

//            var line = await reader.ReadLineAsync();
//            if (line is null)
//            {
//                await Task.Delay(250);
//                continue;
//            }
//            Console.WriteLine(line);
//        }
//    }

//    // ----- OS-specific installers -----

//    private static async Task InstallWindowsAsync(string binPath)
//    {
//        // Ensure path quoted for spaces
//        var bin = $"\"{binPath}\"";
//        // Create service (LocalService account; change to suit)
//        await RunAsync("sc", $"create {ServiceName} binPath= {bin} start= auto DisplayName= \"DamSync\"");
//        await RunAsync("sc", $"description {ServiceName} \"DamSync demo worker\"");
//        Console.WriteLine("Installed Windows Service.");
//    }

//    private static async Task RemoveWindowsAsync()
//    {
//        await RunAsync("sc", $"stop {ServiceName}", ignoreErrors: true);
//        await RunAsync("sc", $"delete {ServiceName}");
//        Console.WriteLine("Removed Windows Service.");
//    }

//    private static async Task InstallSystemdAsync(string binPath)
//    {
//        var unit = $"""
//        [Unit]
//        Description=DamSync demo worker
//        After=network.target

//        [Service]
//        ExecStart={binPath}
//        WorkingDirectory=/
//        Restart=always
//        RestartSec=5
//        User={ServiceUser}
//        Environment=DOTNET_EnableDiagnostics=0

//        [Install]
//        WantedBy=multi-user.target
//        """;

//        var unitPath = "/etc/systemd/system/dam-sync.service";
//        await WriteRootFileAsync(unitPath, unit);
//        await RunAsync("systemctl", "daemon-reload");
//        await RunAsync("systemctl", "enable dam-sync");
//        Console.WriteLine("Installed systemd service at " + unitPath);
//    }

//    private static async Task RemoveSystemdAsync()
//    {
//        await RunAsync("systemctl", "disable dam-sync", ignoreErrors: true);
//        await RunAsync("systemctl", "stop dam-sync", ignoreErrors: true);
//        var unitPath = "/etc/systemd/system/dam-sync.service";
//        try { File.Delete(unitPath); } catch { }
//        await RunAsync("systemctl", "daemon-reload");
//        Console.WriteLine("Removed systemd service.");
//    }

//    private static async Task InstallLaunchdAsync(string binPath)
//    {
//        // For demo we use a user agent (no root required). For system daemon, write to /Library/LaunchDaemons with sudo.
//        var plistId = "com.example.dam-sync";
//        var plist = $"""
//        <?xml version="1.0" encoding="UTF-8"?>
//        <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
//        <plist version="1.0">
//          <dict>
//            <key>Label</key><string>{plistId}</string>
//            <key>ProgramArguments</key>
//            <array><string>{binPath}</string></array>
//            <key>RunAtLoad</key><true/>
//            <key>KeepAlive</key><true/>
//            <key>StandardOutPath</key><string>/usr/local/var/log/dam-sync/dam-sync.log</string>
//            <key>StandardErrorPath</key><string>/usr/local/var/log/dam-sync/dam-sync.log</string>
//          </dict>
//        </plist>
//        """;

//        var plistPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "LaunchAgents", "com.example.dam-sync.plist");
//        Directory.CreateDirectory(Path.GetDirectoryName(plistPath)!);
//        await File.WriteAllTextAsync(plistPath, plist);
//        Directory.CreateDirectory("/usr/local/var/log/dam-sync");
//        await RunAsync("launchctl", $"bootstrap gui/{GetLoginUid()} {plistPath}");
//        await RunAsync("launchctl", $"enable gui/{GetLoginUid()}/{plistId}");
//        Console.WriteLine("Installed launchd agent at " + plistPath);
//    }

//    private static async Task RemoveLaunchdAsync()
//    {
//        var plistId = "com.example.dam-sync";
//        var plistPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "LaunchAgents", "com.example.dam-sync.plist");
//        await RunAsync("launchctl", $"disable gui/{GetLoginUid()}/{plistId}", ignoreErrors: true);
//        await RunAsync("launchctl", $"bootout gui/{GetLoginUid()}/{plistId}", ignoreErrors: true);
//        try { File.Delete(plistPath); } catch { }
//        Console.WriteLine("Removed launchd agent.");
//    }

//    // ----- helpers -----

//    private static async Task RunAsync(string file, string args, bool ignoreErrors = false)
//    {
//        var psi = new ProcessStartInfo
//        {
//            FileName = file,
//            Arguments = args,
//            RedirectStandardOutput = true,
//            RedirectStandardError = true,
//            UseShellExecute = false
//        };
//        // If sudo is needed on Unix for system install
//        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && (file is "systemctl" || args.Contains("/etc/")))
//        {
//            psi.FileName = "sudo";
//            psi.Arguments = $"{file} {args}";
//        }

//        using var p = Process.Start(psi)!;
//        var stdout = await p.StandardOutput.ReadToEndAsync();
//        var stderr = await p.StandardError.ReadToEndAsync();
//        p.WaitForExit();

//        if (!string.IsNullOrWhiteSpace(stdout)) Console.WriteLine(stdout.TrimEnd());
//        if (p.ExitCode != 0 && !ignoreErrors)
//            throw new InvalidOperationException($"{file} {args} failed ({p.ExitCode}): {stderr}");
//    }

//    private static async Task WriteRootFileAsync(string path, string content)
//    {
//        // write to a temp then sudo mv to correct place to preserve permissions
//        var tmp = Path.GetTempFileName();
//        await File.WriteAllTextAsync(tmp, content);
//        await RunAsync("mv", $"{tmp} {path}");
//    }

//    private static int GetLoginUid()
//    {
//        // best-effort: use environment var on macOS shells; fallback to current uid
//        var uidStr = Environment.GetEnvironmentVariable("UID");
//        if (int.TryParse(uidStr, out var uid)) return uid;
//        return (int)System.Convert.ChangeType(System.Environment.UserName.GetHashCode(), typeof(int));
//    }

//    private static string DefaultLogPath()
//    {
//        // Use user-writable locations in Development to avoid access-denied errors
//        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//        {
//            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
//            return Path.Combine(root, "DamSync", "logs", "dam-sync.log");
//        }
//        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
//        {
//            // Preferred per XDG-like layout for dev: ~/.local/state/dam-sync/logs
//            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
//            string preferred = Path.Combine(home, ".local", "state", "dam-sync", "logs", "dam-sync.log");
//            return preferred;
//        }
//        else
//        {
//            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
//            // Try XDG_STATE_HOME if set, otherwise ~/.local/state
//            string? xdgState = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
//            string baseDir = !string.IsNullOrWhiteSpace(xdgState)
//                ? xdgState
//                : Path.Combine(home, ".local", "state");
//            return Path.Combine(baseDir, "dam-sync", "logs", "dam-sync.log");
//        }

//        //if (env.IsDevelopment())
//        //{
//        //    // Use user-writable locations in Development to avoid access-denied errors
//        //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//        //    {
//        //        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
//        //        return Path.Combine(root, "DamSync", "logs", "dam-sync.log");
//        //    }
//        //    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
//        //    {
//        //        // Preferred per XDG-like layout for dev: ~/.local/state/dam-sync/logs
//        //        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
//        //        string preferred = Path.Combine(home, ".local", "state", "dam-sync", "logs", "dam-sync.log");
//        //        return preferred;
//        //    }
//        //    else
//        //    {
//        //        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
//        //        // Try XDG_STATE_HOME if set, otherwise ~/.local/state
//        //        string? xdgState = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
//        //        string baseDir = !string.IsNullOrWhiteSpace(xdgState)
//        //            ? xdgState
//        //            : Path.Combine(home, ".local", "state");
//        //        return Path.Combine(baseDir, "dam-sync", "logs", "dam-sync.log");
//        //    }
//        //}

//        // Non-Development (Production/Staging): keep existing service-friendly paths
//        //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//        //    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DamSync", "logs", "dam-sync.log");
//        //if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
//        //    return "/usr/local/var/log/dam-sync/dam-sync.log";
//        //return "/var/log/dam-sync/dam-sync.log";
//    }
//}

