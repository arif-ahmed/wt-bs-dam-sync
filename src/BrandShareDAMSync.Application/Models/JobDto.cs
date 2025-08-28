using System;

namespace BrandshareDamSync.Application.Models
{
    public sealed class JobDto
    {
        public string Id { get; init; } = default!;
        public string Name { get; init; } = default!;
        public string SourceFolderPath { get; init; } = default!;
        public string DestinationFolderPath { get; init; } = default!;
        public string Status { get; init; } = default!;
        public DateTime CreatedUtc { get; init; }
    }
}
