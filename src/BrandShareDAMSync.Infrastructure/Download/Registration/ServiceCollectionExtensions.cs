using BrandshareDamSync.Infrastructure.Download.Abstractions;
using BrandshareDamSync.Infrastructure.Download.Options;
using BrandshareDamSync.Infrastructure.Download.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BrandshareDamSync.Infrastructure.Download.Registration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDownloadingInfrastructure(
        this IServiceCollection services,
        Action<FileDownloadOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<FileDownloadOptions>(_ => { });

        // Concrete low-level downloader (swap for a fake in tests)
        // services.AddTransient<IDownloaderService, HttpDownloaderService>();

        // Or register HttpClient separately and inject:
        // services.AddHttpClient<HttpDownloaderService>();
        // services.AddSingleton<IDownloaderService>(sp => sp.GetRequiredService<HttpDownloaderService>());

        // High-level orchestrator (throttle, de-dupe, retries, atomic replace)
        services.AddSingleton<IFileDownloaderService, FileDownloaderService>();

        return services;
    }
}
