namespace BrandshareDamSync.Daemon.Common
{
    public sealed class ApiKeyHandler : DelegatingHandler
    {
        private readonly string _apiKey;
        public ApiKeyHandler(IConfiguration cfg)
        {
            _apiKey = cfg["BrandShareDam:ApiKey"] ?? "";
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                request.Headers.Remove("X-ApiKey");
                request.Headers.Add("X-ApiKey", _apiKey);
            }
            return base.SendAsync(request, ct);
        }
    }
}
