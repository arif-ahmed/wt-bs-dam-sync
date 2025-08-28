using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace BrandshareDamSync.Infrastructure.FileProcessors
{
    public class FileDownloader
    {
        private readonly HttpClient _httpClient;

        public FileDownloader(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Downloads a file from the specified URL and saves it to the given local path.
        /// </summary>
        /// <param name="fileUrl">The URL of the file to download.</param>
        /// <param name="destinationPath">The local path to save the downloaded file.</param>
        /// <returns>True if download succeeds, otherwise false.</returns>
        public async Task<bool> DownloadFileAsync(string fileUrl, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("File URL cannot be null or empty.", nameof(fileUrl));
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("Destination path cannot be null or empty.", nameof(destinationPath));

            try
            {
                using var response = await _httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fileStream);

                return true;
            }
            catch (Exception)
            {
                // Log exception as needed
                return false;
            }
        }
    }
}
