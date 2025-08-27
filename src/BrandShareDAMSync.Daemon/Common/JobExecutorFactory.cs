using BrandshareDamSync.Abstractions;

namespace BrandshareDamSync.Daemon.Common;

public sealed class JobExecutorFactory
{
    private readonly IServiceProvider _sp;
    public JobExecutorFactory(IServiceProvider sp) => _sp = sp;

    public IJobExecutorService CreateJobExecutorService(string jobType) =>
        _sp.GetRequiredKeyedService<IJobExecutorService>(jobType);
}
