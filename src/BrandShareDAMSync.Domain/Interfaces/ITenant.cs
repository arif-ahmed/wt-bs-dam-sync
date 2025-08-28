namespace BrandshareDamSync.Domain.Interfaces;
public interface ITenant
{
    public string TenantId { get; set; }
    public Tenant Tenant { get; set; }
}
