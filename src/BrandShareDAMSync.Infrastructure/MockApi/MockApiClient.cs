using Newtonsoft.Json;
using System.Text;

namespace BrandshareDamSync.Infrastructure.MockApi;


public sealed record CreateDamFolderDto(string JobId, string ParentFolderId, string FolderName);

public sealed record CreateDamFolderResponse(string folderId, string status);

public class MockApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        Formatting = Formatting.None
    };

    public MockApiClient(HttpClient http)
    {
        _http = http;
    }

    // ---------- Generic Helpers ----------

    public async Task<T?> GetAsync<T>(string relativePath, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, relativePath);
        using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        await EnsureSuccess(res).ConfigureAwait(false);

        var json = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(json) ? default : JsonConvert.DeserializeObject<T>(json, _jsonSettings);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string relativePath, TRequest body, CancellationToken ct = default)
    {
        var jsonBody = JsonConvert.SerializeObject(body, _jsonSettings);
        using var req = new HttpRequestMessage(HttpMethod.Post, relativePath)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        await EnsureSuccess(res).ConfigureAwait(false);

        var json = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(json) ? default : JsonConvert.DeserializeObject<TResponse>(json, _jsonSettings);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, relativePath);
        var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        await EnsureSuccess(res).ConfigureAwait(false);
        return res;
    }

    // ---------- Shared Error Helper ----------

    private static async Task EnsureSuccess(HttpResponseMessage res)
    {
        if (res.IsSuccessStatusCode) return;

        string? body = null;
        try { body = await res.Content.ReadAsStringAsync().ConfigureAwait(false); }
        catch { /* ignore if reading fails */ }

        var detail = string.IsNullOrWhiteSpace(body) ? "" : $" Body: {body}";
        throw new HttpRequestException(
            $"Mock API request failed: {(int)res.StatusCode} {res.ReasonPhrase}.{detail}");
    }

    // ---------- Example Typed Methods ----------

    //public Task<HealthDto?> CreateDamFolder(CancellationToken ct = default)
    //    => GetAsync<HealthDto>("health", ct);

    public Task<CreateDamFolderResponse?> CreateDamFolderAsync(CreateDamFolderDto dto, CancellationToken ct = default)
    {
        var qs = BuildQueryString(new()
        {
            ["jobId"] = dto.JobId,
            ["parentFolderId"] = dto.ParentFolderId,
            ["folderName"] = dto.FolderName
        });

        var relative = $"FileSync/CreateFolder/?{qs}";

        // Calls GET /FileSync/CreateFolder/?jobId=...&parentFolderId=...&folderName=...
        return GetAsync<CreateDamFolderResponse>(relative, ct);
    }

    private static string BuildQueryString(Dictionary<string, string> parameters) =>
    string.Join("&", parameters.Select(kv =>
        $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? string.Empty)}"));

}
