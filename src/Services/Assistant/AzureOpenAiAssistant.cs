using BrandshareDamSync.Infrastructure.Secrets;

namespace BrandshareDamSync.Services.Assistant;

public sealed class AzureOpenAiAssistant : IAssistant
{
    private readonly ISecretStore _secrets;
    public AzureOpenAiAssistant(ISecretStore secrets) => _secrets = secrets;

    public async Task<string> AskAsync(string prompt, CancellationToken ct)
    {
        var key = await _secrets.GetAsync("azureopenai:key", ct);
        var endpoint = await _secrets.GetAsync("azureopenai:endpoint", ct);
        var dep = await _secrets.GetAsync("azureopenai:deployment", ct);

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(dep))
            return $"[stubbed AI] You asked: '{prompt}'. Configure Azure OpenAI to enable live answers.";
        return $"[pretend AI] Thanks for your question about: {prompt}";
    }
}
