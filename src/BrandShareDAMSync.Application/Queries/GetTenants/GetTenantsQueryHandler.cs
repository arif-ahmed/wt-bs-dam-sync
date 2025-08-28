using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Application.Models;
using MediatR;

namespace BrandshareDamSync.Application.Queries.GetTenants;
public class GetTenantsQueryHandler(ITenantRepository repository) : IRequestHandler<GetTenantsQuery, List<TenantDto>>
{
    public async Task<List<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await repository.GetAllAsync();

        var tenantDtos = tenants.Select(t => new TenantDto(t.Id, t.Domain, t.BaseUrl, t.ApiKey)).ToList();
        return tenantDtos;

    }
}
