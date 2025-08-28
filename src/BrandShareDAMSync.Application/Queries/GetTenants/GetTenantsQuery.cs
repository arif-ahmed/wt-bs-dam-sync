using MediatR;
using BrandshareDamSync.Application.Models;

namespace BrandshareDamSync.Application.Queries.GetTenants;

public class GetTenantsQuery : IRequest<List<TenantDto>>
{
    //public string TenantId { get; set; } = default!;
    //public string Domain { get; set; } = default!;
    //public string BaseUrl { get; set; } = default!;
    //public string ApiKey { get; set; } = default!;
}
