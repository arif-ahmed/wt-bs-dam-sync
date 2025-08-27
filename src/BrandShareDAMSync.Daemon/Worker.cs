using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Application.Queries.GetTenants;
using BrandshareDamSync.Daemon.Infrastructure.Http;
using BrandshareDamSync.Daemon.Mappers;
using BrandshareDamSync.Domain;
using BrandshareDamSync.Infrastructure.BrandShareDamClient;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BrandshareDamSync.Daemon;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITenantContext _tenantContext;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, IServiceScopeFactory scopeFactory, ITenantContext tenantContext)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _tenantContext = tenantContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Poll every N minutes; make this configurable via appsettings
        var interval = TimeSpan.FromMinutes(5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var api = scope.ServiceProvider.GetRequiredService<IBrandShareDamApi>();

                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>(); // Ensure TenantConfigStore is registered

                var tenants = await mediator.Send(new GetTenantsQuery(), stoppingToken);

                foreach (var t in tenants)
                {
                    _logger.LogInformation("Polling jobs for tenant {Tenant}", t.Id);
                    var hostName = "DHA11KHONDOAHM2"; // Environment.MachineName;
                    var resp = await api.GetJobList(t.Id, hostName); // handler picks up BaseUrl+ApiKey from context

                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("GetJobList failed for {Tenant}: {Status} {Reason}",
                            t.Id, (int)resp.StatusCode, resp.ReasonPhrase);
                    }
                    else
                    {
                        _logger.LogInformation("Got {Count} jobs for {Tenant}.", resp.Content?.Jobs?.Count ?? 0, t.Id);

                        if (resp.Content?.Jobs is { Count: > 0 } jobs)
                        {
                            // 1) De-duplicate the incoming list by Id (protects against API duplicates)
                            var distinctJobs = jobs
                                .GroupBy(j => j.Id)
                                .Select(g => g.First())
                                .ToList();

                            foreach (var job in distinctJobs)
                            {
                                var jobDbInfo = SyncJobMapper.ToEntity(job, t.Id);

                                // 2) Look up by Id (include soft-deleted rows)

                                var existingById = await uow.JobRepository.FindAsync(
                                    j => j.Id == jobDbInfo.Id && j.TenantId == t.Id,
                                    cancellationToken: stoppingToken);

                                if (existingById.Item1.Any())
                                {
                                    // Update the tracked entity rather than creating a new one
                                    var entity = existingById.Item1.First();                                    
                                    await uow.JobRepository.UpdateAsync(entity);
                                }
                                else
                                {
                                    // 3) Insert new
                                    await uow.JobRepository.AddAsync(jobDbInfo, stoppingToken);
                                }
                            }

                            // 4) Save once
                            await uow.SaveChangesAsync(stoppingToken);
                        }


                        // … your sync logic …
                    }

                }

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync cycle failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException) 
            {
                // Task was cancelled, exit gracefully
                _logger.LogInformation("Worker stopping as task was cancelled.");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during delay");
            }
        }
    }
}
