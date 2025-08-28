namespace BrandshareDamSync.Domain;
public abstract class EntityBase
{
    public string Id { get; set; } = default!;
    public bool IsDeleted { get; set; }
}
