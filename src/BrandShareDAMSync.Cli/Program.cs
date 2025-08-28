// <Project Sdk="Microsoft.NET.Sdk">
//   <PropertyGroup>
//     <OutputType>Exe</OutputType>
//     <TargetFramework>net8.0</TargetFramework>
//     <Nullable>enable</Nullable>
//     <ImplicitUsings>enable</ImplicitUsings>
//   </PropertyGroup>
//   <ItemGroup>
//     <PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
//     <PackageReference Include="Spectre.Console" Version="0.49.1" />
//   </ItemGroup>
// </Project>

using BrandshareDamSync.Application;
using BrandshareDamSync.Infrastructure.Persistence;
using BrandshareDamSync.Infrastructure.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using System.CommandLine;

namespace BrandshareDamSync.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, cfg) =>
            {
                var env = ctx.HostingEnvironment; // ? here
                cfg.SetBasePath(AppContext.BaseDirectory)
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                   .AddEnvironmentVariables();
            })
            .ConfigureServices((ctx, services) =>
            {
                // env also available here via ctx.HostingEnvironment
                services.AddPersistence(ctx.Configuration);
                services.AddScoped<TenantCommandHandlers>();
                services.AddApplication(); // adds MediatR and other application services
            })
            .Build();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DamSyncDbContext>();
            await db.Database.MigrateAsync(); // ensures the Tenants table exists
        }

        // Touch IMediator so Application is wired (optional but nice)
        _ = host.Services.GetRequiredService<IMediator>();

        // Root command (default = interactive tenants menu)
        var root = new RootCommand("dam-sync — tenants only");

        // Default: interactive menu
        root.SetHandler(async () =>
        {
            await TenantInteractiveMenu.ShowAsync(host.Services);
        });

        // ---------- Non-interactive `tenants` command group ----------
        var tenants = new Command("tenants", "Manage tenants (CRUD).");

        // tenants list
        var listCmd = new Command("list", "List all tenants");
        listCmd.SetHandler(async () =>
        {
            using var scope = host.Services.CreateScope();
            var h = scope.ServiceProvider.GetRequiredService<TenantCommandHandlers>();
            await h.ListAsync();
        });
        tenants.AddCommand(listCmd);

        // tenants add
        var addCmd = new Command("add", "Add a tenant");
        var addBaseUrlOpt = new Option<string>("--base-url", "Tenant base URL") { IsRequired = true };
        var addApiKeyOpt = new Option<string>("--api-key", "Tenant API key") { IsRequired = true };
        var addDomainOpt = new Option<string?>("--domain", () => null, "Optional tenant domain");
        addCmd.AddOption(addBaseUrlOpt);
        addCmd.AddOption(addApiKeyOpt);
        addCmd.AddOption(addDomainOpt);

        addCmd.SetHandler(
            async (string baseUrl, string apiKey, string? domain) =>
            {
                using var scope = host.Services.CreateScope();
                var h = scope.ServiceProvider.GetRequiredService<TenantCommandHandlers>();
                await h.AddAsync(baseUrl, apiKey, domain);
            },
            addBaseUrlOpt, addApiKeyOpt, addDomainOpt
        );
        tenants.AddCommand(addCmd);

        // tenants update
        var updateCmd = new Command("update", "Update a tenant");
        var updIdOpt = new Option<string>("--id", "Tenant ID") { IsRequired = true };
        var updBaseUrlOpt = new Option<string?>("--base-url", () => null, "New base URL");
        var updApiKeyOpt = new Option<string?>("--api-key", () => null, "New API key");
        var updDomainOpt = new Option<string?>("--domain", () => null, "New domain");
        updateCmd.AddOption(updIdOpt);
        updateCmd.AddOption(updBaseUrlOpt);
        updateCmd.AddOption(updApiKeyOpt);
        updateCmd.AddOption(updDomainOpt);

        updateCmd.SetHandler(
            async (string id, string? baseUrl, string? apiKey, string? domain) =>
            {
                using var scope = host.Services.CreateScope();
                var h = scope.ServiceProvider.GetRequiredService<TenantCommandHandlers>();
                await h.UpdateAsync(id, baseUrl, apiKey, domain);
            },
            updIdOpt, updBaseUrlOpt, updApiKeyOpt, updDomainOpt
        );
        tenants.AddCommand(updateCmd);

        // tenants delete
        var deleteCmd = new Command("delete", "Delete a tenant");
        var delIdOpt = new Option<string>("--id", "Tenant ID") { IsRequired = true };
        deleteCmd.AddOption(delIdOpt);

        deleteCmd.SetHandler(
            async (string id) =>
            {
                using var scope = host.Services.CreateScope();
                var h = scope.ServiceProvider.GetRequiredService<TenantCommandHandlers>();
                await h.DeleteAsync(id);
            },
            delIdOpt
        );
        tenants.AddCommand(deleteCmd);

        root.AddCommand(tenants);



        try
        {
            return await root.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return 1;
        }
    }
}
