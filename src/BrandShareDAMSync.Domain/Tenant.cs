namespace BrandshareDamSync.Domain;
public class Tenant : EntityBase
{
    /// <summary>
    /// Gets or sets the name of the tenant.
    /// </summary>
    public string Domain { get; set; } = default!;

    /// <summary>
    /// Gets or sets the URL of the tenant's BrandShare DAM instance.
    /// </summary>
    public string BaseUrl { get; set; } = default!;

    /// <summary>
    /// Gets or sets the API key for accessing the tenant's BrandShare DAM instance.
    /// </summary>
    public string ApiKey { get; set; } = default!;

    #region navigation properties
    public IEnumerable<SyncJob> Jobs { get; set; } = new List<SyncJob>();
    #endregion
}
