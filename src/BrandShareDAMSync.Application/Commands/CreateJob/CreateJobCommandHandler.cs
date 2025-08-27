using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using BrandshareDamSync.Abstractions;
using BrandshareDamSync.Domain;

namespace BrandshareDamSync.Application.Commands.CreateJob
{
    public sealed class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, string>
    {
        private readonly IJobRepository _jobs;
        private readonly IUnitOfWork _uow;

        public CreateJobCommandHandler(IJobRepository jobs, IUnitOfWork uow)
        {
            _jobs = jobs;
            _uow = uow;
        }

        public async Task<string> Handle(CreateJobCommand request, CancellationToken ct)
        {
            // var job = Job.Create(
            //     name: request.Name,
            //     sourceFolderPath: request.SourceFolderPath,
            //     destinationFolderPath: request.DestinationFolderPath
            // );

            var job = new SyncJob
            {
                //Name = request.Name,
                //SourceFolderPath = request.SourceFolderPath,
                //DestinationFolderPath = request.DestinationFolderPath,
                //CreatedAt = DateTime.UtcNow,
                //UpdatedAt = DateTime.UtcNow
            };

            await _jobs.AddAsync(job, ct);
            await _uow.SaveChangesAsync(ct);

            return job.Id;
        }
    }
}
