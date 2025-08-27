using System;
using System.IO;
using System.Threading.Tasks;

namespace BrandshareDamSync.Infrastructure.FileProcessors
{
    public class FileUploader
    {
        private readonly string _destinationDirectory;

        public FileUploader(string destinationDirectory)
        {
            if (string.IsNullOrWhiteSpace(destinationDirectory))
                throw new ArgumentException("Destination directory cannot be null or empty.", nameof(destinationDirectory));

            _destinationDirectory = destinationDirectory;
            Directory.CreateDirectory(_destinationDirectory);
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            string destinationPath = Path.Combine(_destinationDirectory, fileName);

            using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await fileStream.CopyToAsync(destinationStream);
            }

            return destinationPath;
        }
    }
}
