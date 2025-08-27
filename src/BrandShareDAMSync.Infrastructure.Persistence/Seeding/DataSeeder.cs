
using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection.PortableExecutable;

namespace BrandshareDamSync.Infrastructure.Persistence.Seeding;

public sealed class DataSeeder(DamSyncDbContext db, ILogger<DataSeeder> logger) : IDataSeeder
{
    private readonly DamSyncDbContext _db = db;
    private readonly ILogger<DataSeeder> _logger = logger;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedTenantsAsync(ct);

        // add more steps here…
    }

    private async Task SeedTenantsAsync(CancellationToken ct)
    {
        // Example: ensure a demo tenant exists (replace with your real defaults)
        string tenantId = Guid.NewGuid().ToString("N");

        if (!_db.Tenants.Any())
        {
            _db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Domain = "Rony Demo",
                BaseUrl = "https://ronydemo.marcombox.com",
                ApiKey = "akDBz4SxA1CSDjN1lVIYx7Hi6/9jPi56qa2PtnMaIen20BzaTMa1oO/H4PIgXwep7r/Xw39M4ld7Jsohpi1+vCCw7ek8uqXC3x/7HJTRRal5VqNAASY1b+3lVBbNdwFB"

            });
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded tenant '{TenantId}'.", tenantId);
        }
    }
}
