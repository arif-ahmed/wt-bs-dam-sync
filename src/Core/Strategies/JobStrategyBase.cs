namespace BrandshareDamSync.Core.Strategies;

public abstract class JobStrategyBase : IJobStrategy
{
    public abstract BrandshareDamSync.Core.Models.JobDirection Direction { get; }

    protected async Task ForEachFileAsync(string root, Func<string, Task> act, CancellationToken ct)
    {
        if (!Directory.Exists(root)) return;
        var stack = new Stack<string>(); stack.Push(root);
        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var current = stack.Pop();
            foreach (var d in Directory.EnumerateDirectories(current)) stack.Push(d);
            foreach (var f in Directory.EnumerateFiles(current)) await act(f);
        }
    }

    public abstract Task ExecuteAsync(BrandshareDamSync.Core.Models.Job job, BrandshareDamSync.Infrastructure.Dam.IDamClient dam, BrandshareDamSync.Core.Models.RuntimeState state, CancellationToken ct);
}
