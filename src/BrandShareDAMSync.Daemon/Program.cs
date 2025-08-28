using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Application;
using BrandshareDamSync.Daemon;
using BrandshareDamSync.Daemon.Common;
using BrandshareDamSync.Daemon.Infrastructure.Http;
using BrandshareDamSync.Daemon.JobExecutors;
using BrandshareDamSync.Daemon.Mappers;
using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure;
using BrandshareDamSync.Infrastructure.BrandShareDamClient;
using BrandshareDamSync.Infrastructure.Download.Services;
using BrandshareDamSync.Infrastructure.JobExecutors;
using BrandshareDamSync.Infrastructure.Persistence;
using BrandshareDamSync.Infrastructure.Persistence.Data;
using BrandshareDamSync.Infrastructure.Persistence.Seeding;
using BrandshareDamSync.Infrastructure.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.WindowsServices;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Refit;
using System;
using System.Net;
using System.Runtime.InteropServices;

// Minimal Serilog config: file + event log
//Log.Logger = new LoggerConfiguration()
//    .Enrich.FromLogContext()
//    .WriteTo.File("logs\\service.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
//    .WriteTo.EventLog("BrandshareDamSync", manageEventSource: true)
//    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Force-load config from the executing directory (works for CLI, Windows Service, systemd, EF tools)
    builder.Configuration
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables();


    //builder.Services.AddDbContext<DamSyncDbContext>(opts =>
    //{
    //    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    //    opts.UseSqlite(connectionString, sqlOpts =>
    //    {
    //        sqlOpts.MigrationsAssembly(typeof(Program).Assembly.FullName);
    //    });
    //});

    builder.Services
        .AddApplication()
        .AddPersistence(builder.Configuration);



    // Load environment (Development, Production, Staging, etc.)
    var env = builder.Environment;

    if (builder.Environment.IsDevelopment())
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);

        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        builder.Configuration.AddUserSecrets<Program>();
    }

    var apiKey = builder.Configuration["BrandShareDam:ApiKey"];
    // Add AutoMapper configuration
    builder.Services.AddAutoMapper(typeof(DamApiMappingProfile));
    builder.Services.AddKeyedTransient<IJobExecutorService, OneWayDownloadJobExecutor>(SyncJobType.DOWNLOAD);
    builder.Services.AddKeyedTransient<IJobExecutorService, OneWayUploadJobExecutor>(SyncJobType.UPLOAD);
    builder.Services.AddKeyedTransient<IJobExecutorService, BiDirectionalSyncJobExecutor>(SyncJobType.BOTH);
    builder.Services.AddScoped<JobExecutorFactory>();

    builder.Services.AddTransient<IDownloaderService, HttpDownloaderService>();
    builder.Services.AddSingleton(new DownloadCoordinator(maxConcurrent: 3)); // tune this (2�5 is usually safe)
    // builder.Services.AddTransient<S3FileUploader>();


    #region refit, polly, api-key configuration for BrandShare DAM API
    // Caching + tenant services
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ITenantContext, TenantContext>();
    builder.Services.AddTransient<ITenantConfigStore, SqliteTenantConfigStore>();
    builder.Services.AddTransient<TenantConfigHandler>();

    // Refit client with dynamic base address + api key
    builder.Services
        .AddRefitClient<IBrandShareDamApi>(new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer()
        })
        .ConfigureHttpClient(c =>
        {
            // MUST be set so Refit's relative routes are valid.
            c.BaseAddress = new Uri("http://example.invalid/"); // RFC 2606 reserved TLD
            c.Timeout = TimeSpan.FromMinutes(2);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(5))
        .AddHttpMessageHandler<TenantConfigHandler>()
        // Resilience policies unchanged
        .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => (int)r.StatusCode == 429)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250))
        ))
        .AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => (int)r.StatusCode == 429)
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(45), TimeoutStrategy.Pessimistic));
    #endregion

    #region background services
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddHostedService<MachinePoller>();
    #endregion

    #region platform-detection
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        // Only use Windows Service lifetime when actually running as a service
        if (WindowsServiceHelpers.IsWindowsService())
        {
            builder.Services.AddWindowsService(options => options.ServiceName = "DamSync");
        }
    }
    else
    {
        builder.Services.AddSystemd(); // no-op on non-systemd
    }
    #endregion

    #region logging
    string logPath = LoggerPath.GetLogPath(env);
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

    //builder.Logging.ClearProviders();
    //builder.Logging.AddSerilog();

    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(o => { o.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; });
    builder.Logging.AddFileLogger(logPath); // tiny in-file logger below
    #endregion

    var host = builder.Build();

    // ?? run migrations here
    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<DamSyncDbContext>();
        await db.Database.MigrateAsync();


        // ?? run seeding
        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync();
    }

    await host.RunAsync();

}
catch (Exception ex)
{
    // ignore
}
finally
{
    // Log.CloseAndFlush();
}

//static class LoggerPath
//{
//    public static string GetDefaultLogPath()
//    {
//        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DamSync", "logs", "dam-sync.log");
//        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
//            return "/usr/local/var/log/dam-sync/dam-sync.log";
//        return "/var/log/dam-sync/dam-sync.log";
//    }
//}

static class LoggerPath
{
    public static string GetLogPath(IHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            // Use user-writable locations in Development to avoid access-denied errors
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(root, "DamSync", "logs", "dam-sync.log");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Preferred per XDG-like layout for dev: ~/.local/state/dam-sync/logs
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string preferred = Path.Combine(home, ".local", "state", "dam-sync", "logs", "dam-sync.log");
                return preferred;
            }
            else
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                // Try XDG_STATE_HOME if set, otherwise ~/.local/state
                string? xdgState = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
                string baseDir = !string.IsNullOrWhiteSpace(xdgState)
                    ? xdgState
                    : Path.Combine(home, ".local", "state");
                return Path.Combine(baseDir, "dam-sync", "logs", "dam-sync.log");
            }
        }

        // Non-Development (Production/Staging): keep existing service-friendly paths
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DamSync", "logs", "dam-sync.log");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "/usr/local/var/log/dam-sync/dam-sync.log";
        return "/var/log/dam-sync/dam-sync.log";
    }
}

// Minimal file logger provider (for the demo)
public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, string path)
    {
        builder.AddProvider(new FileLoggerProvider(path));
        return builder;
    }
}

file sealed class FileLoggerProvider(string path) : ILoggerProvider
{
    private readonly StreamWriter _writer = new(File.Open(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
    public ILogger CreateLogger(string categoryName) => new FileLogger(_writer, categoryName);
    public void Dispose() => _writer.Dispose();
    private sealed class FileLogger(StreamWriter writer, string category) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new DummyDisposable();

        private sealed class DummyDisposable : IDisposable
        {
            public void Dispose() { }
        }
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {category}: {formatter(state, exception)}";
            writer.WriteLine(line);
            if (exception != null) writer.WriteLine(exception);
        }
    }
}

#region extra
/**
 * using BrandshareDamSync.Application;
using BrandshareDamSync.Infrastructure;
using Microsoft.Extensions.Hosting;
using Serilog;

// Minimal Serilog config: file + event log
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.File("logs\\service.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .WriteTo.EventLog("BrandshareDamSync", manageEventSource: true)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    builder.Services.AddHostedService<Worker>(); // your existing Worker

    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "BrandShare DAM Sync";
    });

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

*/
#endregion

