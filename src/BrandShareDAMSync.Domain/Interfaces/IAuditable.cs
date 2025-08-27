namespace BrandshareDamSync.Domain.Interfaces;
public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; set; }
    DateTimeOffset LastModifiedAtUtc { get; set; }
}
