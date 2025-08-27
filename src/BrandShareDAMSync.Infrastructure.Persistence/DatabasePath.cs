using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;


namespace BrandshareDamSync.Infrastructure.Persistence;

static class DatabasePath
{
    private const string FileName = "dam-sync.db";

    /// <summary>
    /// Returns a user-writable or service-friendly absolute path for the SQLite DB,
    /// following the same OS-specific pattern as LoggerPath.
    /// </summary>
    public static string GetDbPath(IHostEnvironment env)
    {
        // Highest-precedence override via environment variable (optional but handy)
        // e.g. DAMSYNC_DB_PATH=D:\data\dam-sync.db
        var fromEnv = Environment.GetEnvironmentVariable("DAMSYNC_DB_PATH");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return EnsureDirectoryAndReturn(fromEnv);

        if (env.IsDevelopment())
        {
            // Development: user-writable locations to avoid permission headaches
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return EnsureDirectoryAndReturn(Path.Combine(root, "DamSync", "db", FileName));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Match your dev logs convention (XDG-like under ~/.local/state)
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return EnsureDirectoryAndReturn(Path.Combine(home, ".local", "state", "dam-sync", "db", FileName));
            }
            else
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string? xdgState = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
                string baseDir = !string.IsNullOrWhiteSpace(xdgState)
                    ? xdgState
                    : Path.Combine(home, ".local", "state");
                return EnsureDirectoryAndReturn(Path.Combine(baseDir, "dam-sync", "db", FileName));
            }
        }

        // Non-Development (Production/Staging): service-friendly paths
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return EnsureDirectoryAndReturn(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                             "DamSync", "db", FileName));
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Parallel to your /usr/local/var/log path for logs
            return EnsureDirectoryAndReturn("/usr/local/var/lib/dam-sync/dam-sync.db");
        }
        // Linux
        return EnsureDirectoryAndReturn("/var/lib/dam-sync/dam-sync.db");
    }

    /// <summary>
    /// For design-time usage where IHostEnvironment isn't available.
    /// Pass e.g. DOTNET_ENVIRONMENT.
    /// </summary>
    public static string GetDbPath(string environmentName)
    {
        var isDev = string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
        // Fake a minimal host-env decision:
        var fakeEnv = new FakeHostEnvironment(isDev);
        return GetDbPath(fakeEnv);
    }

    private static string EnsureDirectoryAndReturn(string fullPath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(fullPath))!;
        Directory.CreateDirectory(dir);
        return Path.GetFullPath(fullPath);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(bool isDev, string? contentRoot = null)
        {
            EnvironmentName = isDev ? Environments.Development : Environments.Production;
            ApplicationName = "DamSync";
            ContentRootPath = contentRoot ?? AppContext.BaseDirectory;

            // Use a no-op provider to avoid extra package references.
            // If you prefer a real physical provider, see note below.
            ContentRootFileProvider = new NullFileProvider();
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }

}
