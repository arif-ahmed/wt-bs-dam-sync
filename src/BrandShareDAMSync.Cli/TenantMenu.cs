using BrandshareDamSync.Abstractions; // ITenantRepository, IUnitOfWork (adjust if your namespaces differ)
using BrandshareDamSync.Domain;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
// using BrandshareDamSync.Domain;     // Tenant entity type (adjust import if needed)

namespace BrandshareDamSync.Cli;

public static class TenantInteractiveMenu
{
    public static async Task ShowAsync(IServiceProvider services)
    {
        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Tenants — choose an action[/]")
                    .PageSize(10)
                    .AddChoices("List", "Add", "Update", "Delete", "Back"));

            try
            {
                switch (choice)
                {
                    case "List": await ListAsync(services); break;
                    case "Add": await AddAsync(services); break;
                    case "Update": await UpdateAsync(services); break;
                    case "Delete": await DeleteAsync(services); break;
                    case "Back": return;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            }
        }
    }

    private static async Task ListAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var tenants = await repo.GetAllAsync();

        if (tenants is null || !tenants.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No tenants found.[/]");
            Pause();
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Id");
        table.AddColumn("BaseUrl");
        table.AddColumn("Domain");
        table.AddColumn("ApiKey (masked)");

        foreach (var t in tenants)
        {
            var masked = string.IsNullOrEmpty(t.ApiKey)
                ? ""
                : (t.ApiKey.Length <= 6 ? new string('*', t.ApiKey.Length)
                                        : $"{t.ApiKey[..3]}***{t.ApiKey[^3..]}");

            table.AddRow(t.Id.ToString(), t.BaseUrl ?? "", t.Domain ?? "", masked);
        }

        AnsiConsole.Write(table);
        Pause();
    }

    private static async Task AddAsync(IServiceProvider services)
    {
        var baseUrl = AnsiConsole.Ask<string>("Base URL:");
        var apiKey = AnsiConsole.Prompt(new TextPrompt<string>("API Key:").Secret());
        var domain = AnsiConsole.Ask<string?>("Domain (optional):", null);

        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var tenant = new Tenant
        {
            Id = Guid.NewGuid().ToString(),
            BaseUrl = baseUrl,
            ApiKey = apiKey,
            Domain = string.IsNullOrWhiteSpace(domain) ? null : domain
        };

        await repo.AddAsync(tenant);
        await uow.SaveChangesAsync();

        AnsiConsole.MarkupLine($"[green]? Added[/] Id=[cyan]{tenant.Id}[/]");
        Pause();
    }

    private static async Task UpdateAsync(IServiceProvider services)
    {
        var id = AnsiConsole.Ask<string>("Tenant Id to update:");

        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var t = await repo.GetByIdAsync(id);
        if (t is null)
        {
            AnsiConsole.MarkupLine("[red]Tenant not found.[/]");
            Pause();
            return;
        }

        var newBaseUrl = AnsiConsole.Ask<string>($"Base URL ([grey]current: {t.BaseUrl}[/])", t.BaseUrl ?? "");
        var newApiKey = AnsiConsole.Prompt(new TextPrompt<string>("API Key ([grey]leave empty to keep[/]):").AllowEmpty());
        var newDomain = AnsiConsole.Ask<string?>($"Domain ([grey]current: {t.Domain ?? "<none>"}[/])", t.Domain);

        t.BaseUrl = newBaseUrl;
        if (!string.IsNullOrWhiteSpace(newApiKey))
            t.ApiKey = newApiKey;
        t.Domain = string.IsNullOrWhiteSpace(newDomain) ? null : newDomain;

        await repo.UpdateAsync(t);
        await uow.SaveChangesAsync();

        AnsiConsole.MarkupLine("[green]? Updated[/]");
        Pause();
    }

    private static async Task DeleteAsync(IServiceProvider services)
    {
        var id = AnsiConsole.Ask<string>("Tenant Id to delete:");
        if (!AnsiConsole.Confirm($"Delete tenant [yellow]{id}[/]?")) return;

        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await repo.DeleteAsync(id);
        await uow.SaveChangesAsync();

        AnsiConsole.MarkupLine("[green]? Deleted[/]");
        Pause();
    }

    private static void Pause()
    {
        AnsiConsole.MarkupLine("\nPress any key to continue...");
        Console.ReadKey(intercept: true);
    }
}

/// <summary>
/// Non-interactive handlers used by `dam-sync tenants ...` subcommands.
/// </summary>
public sealed class TenantCommandHandlers
{
    private readonly IServiceProvider _services;
    public TenantCommandHandlers(IServiceProvider services) => _services = services;

    public async Task ListAsync()
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var tenants = await repo.GetAllAsync();

        if (tenants is null || !tenants.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No tenants found.[/]");
            return;
        }

        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddColumn(new GridColumn());
        grid.AddColumn(new GridColumn());
        grid.AddRow("[bold]Id[/]", "[bold]BaseUrl[/]", "[bold]Domain[/]");
        foreach (var t in tenants)
            grid.AddRow(t.Id.ToString(), t.BaseUrl ?? "", t.Domain ?? "");
        AnsiConsole.Write(grid);
    }

    public async Task AddAsync(string baseUrl, string apiKey, string? domain)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var t = new Tenant
        {
            Id = Guid.NewGuid().ToString(),
            BaseUrl = baseUrl,
            ApiKey = apiKey,
            Domain = string.IsNullOrWhiteSpace(domain) ? null : domain
        };

        await repo.AddAsync(t);
        await uow.SaveChangesAsync();
        AnsiConsole.MarkupLine($"[green]? Added[/] Id=[cyan]{t.Id}[/]");
    }

    public async Task UpdateAsync(string id, string? baseUrl, string? apiKey, string? domain)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var t = await repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Tenant not found.");

        if (!string.IsNullOrWhiteSpace(baseUrl)) t.BaseUrl = baseUrl;
        if (!string.IsNullOrWhiteSpace(apiKey)) t.ApiKey = apiKey;
        if (domain is not null) t.Domain = string.IsNullOrWhiteSpace(domain) ? null : domain;

        await repo.UpdateAsync(t);
        await uow.SaveChangesAsync();
        AnsiConsole.MarkupLine("[green]? Updated[/]");
    }

    public async Task DeleteAsync(string id)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await repo.DeleteAsync(id);
        await uow.SaveChangesAsync();
        AnsiConsole.MarkupLine("[green]? Deleted[/]");
    }
}
