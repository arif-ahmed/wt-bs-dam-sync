using BrandshareDamSync.Core.Models;
using BrandshareDamSync.Core.Scheduler;
using BrandshareDamSync.Core.Strategies;
using BrandshareDamSync.Infrastructure.Config;
using BrandshareDamSync.Infrastructure.Dam;
using BrandshareDamSync.Infrastructure.Secrets;
using BrandshareDamSync.Services.Assistant;

namespace BrandshareDamSync.App;

public sealed class AppContextContainer
{
    public IConfigStore Config { get; }
    public ISecretStore Secrets { get; }
    public IDamClient Dam { get; }
    public RuntimeState State { get; } = new();
    public Scheduler Scheduler { get; }
    public IAssistant Assistant { get; }

    public AppContextContainer()
    {
        Config = new JsonConfigStore();
        Secrets = new SimpleSecretStore();
        Dam = new FakeDamClient();
        var strategies = new IJobStrategy[]
        {
            new OneWayUploadJob(), new UploadAndCleanJob(),
            new OneWayDownloadJob(), new DownloadAndCleanJob(),
            new BiDirectionalSyncJob()
        };
        Scheduler = new Scheduler(Config, Dam, State, strategies);
        Assistant = new AzureOpenAiAssistant(Secrets);
    }
}
