using System.Threading;
using System.Threading.Tasks;
using MediatR;
using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Application.Models;

namespace BrandshareDamSync.Application.Queries.GetJobById
{
    public sealed class GetJobByIdQueryHandler : IRequestHandler<GetJobByIdQuery, JobDto?>
    {
        private readonly IJobRepository _jobs;

        public GetJobByIdQueryHandler(IJobRepository jobs)
        {
            _jobs = jobs;
        }

        public async Task<JobDto?> Handle(GetJobByIdQuery request, CancellationToken ct)
        {
            var job = await _jobs.GetByIdAsync(request.Id, ct);
            if (job is null) return null;

            return new JobDto
            {
                //Id = job.Id,
                //Name = job.Name,
                //SourceFolderPath = job.SourceFolderPath,
                //DestinationFolderPath = job.DestinationFolderPath,
                //Status = job.Status.ToString(),
                //CreatedUtc = job.CreatedUtc
            };
        }
    }
}
