namespace BrandshareDamSync.Services.Assistant;

public interface IAssistant
{
    Task<string> AskAsync(string prompt, CancellationToken ct);
}
