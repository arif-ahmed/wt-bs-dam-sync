using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Infrastructure.Persistence.Data;
using BrandshareDamSync.Infrastructure.Persistence.Data.Interceptors;
using BrandshareDamSync.Infrastructure.Persistence.Repositories;
using BrandshareDamSync.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BrandshareDamSync.Infrastructure.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DamSyncDbContext>((sp, options) =>
            {
                var env = sp.GetRequiredService<IHostEnvironment>();

                // If a full connection string is configured, use it; otherwise build one from our path helper.
                var configured = configuration.GetConnectionString("DefaultConnection");
                string connectionString;
                if (!string.IsNullOrWhiteSpace(configured) && configured.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
                {
                    connectionString = configured!;
                }
                else
                {
                    var dbPath = DatabasePath.GetDbPath(env);
                    connectionString = $"Data Source={dbPath}";
                }

                options.UseSqlite(connectionString, x =>
                {
                    x.MigrationsAssembly("BrandshareDamSync.Infrastructure.Persistence");
                });

                // Keep your warnings/config if any
                // options.ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning));

                // Interceptor is resolved from DI (so it gets ILogger + options)
                options.AddInterceptors(sp.GetServices<IInterceptor>());
            });

            services.AddScoped<IDataSeeder, DataSeeder>(); // ?? register seeder

            // --- Repositories + UnitOfWork (write side) ---
            services.AddTransient<ITenantRepository, TenantRepository>();
            services.AddTransient<IJobRepository, JobRepository>();
            services.AddTransient<IDirectoryRepository, DirectoryRepository>();
            services.AddTransient<IFileEntityRepository, FileEntityRepository>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();

            return services;
        }

        private static string InferProviderFromConnectionString(string cs)
        {
            var lower = cs.ToLowerInvariant();

            // Very lightweight inference; adjust if your format differs.
            if (lower.Contains("data source=") && (lower.Contains(".db") || lower.Contains(".sqlite")))
                return "sqlite";

            if (lower.Contains("host=") || lower.Contains("username=") || lower.Contains("userid="))
                return "postgres";

            // Default to SQL Server (common: "Server=" / "Data Source=" with no .db)
            return "sqlserver";
        }
    }
}
