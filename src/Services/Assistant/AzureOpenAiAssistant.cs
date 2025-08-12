using BrandshareDamSync.Infrastructure.Secrets;
using BrandshareDamSync.Services.Assistant;
using System.Net.Http.Json;
using System.Text.Json;

public sealed class AzureOpenAiAssistant : IAssistant
{
    private readonly ISecretStore _secrets;
    private readonly string _endpoint;
    private readonly string _deployment;

    public AzureOpenAiAssistant(ISecretStore secrets)
    {
        _secrets = secrets;

        // Read from appsettings.json
        var settings = LoadFromAppSettings();
        _endpoint = settings.Endpoint ?? throw new InvalidOperationException("AzureOpenAI.Endpoint missing");
        _deployment = settings.Deployment ?? throw new InvalidOperationException("AzureOpenAI.Deployment missing");
    }

    public async Task<string> AskAsync(string prompt, CancellationToken ct)
    {
        var key = await _secrets.GetAsync("azureopenai:key", ct);
        if (string.IsNullOrWhiteSpace(key))
            return "[error]Azure OpenAI API key not configured. Use 'ai-config' or set AZURE_OPENAI_API_KEY.[/]";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("api-key", key);

        var body = new
        {
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = prompt }
            },
            max_tokens = 500
        };

        var url = $"{_endpoint}openai/deployments/{_deployment}/chat/completions?api-version=2024-02-15-preview";
        var response = await client.PostAsJsonAsync(url, body, ct);

        if (!response.IsSuccessStatusCode)
            return $"[error]Azure OpenAI call failed: {response.StatusCode}[/]";

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "[no content]";
    }

    private static (string? Endpoint, string? Deployment) LoadFromAppSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            return (null, null);

        using var fs = File.OpenRead(path);
        using var doc = JsonDocument.Parse(fs);
        if (!doc.RootElement.TryGetProperty("AzureOpenAI", out var aoai))
            return (null, null);

        string? endpoint = aoai.TryGetProperty("Endpoint", out var ep) ? ep.GetString() : null;
        string? deployment = aoai.TryGetProperty("Deployment", out var dp) ? dp.GetString() : null;

        return (endpoint?.Trim(), deployment?.Trim());
    }
}
