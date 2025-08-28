using MediatR;

namespace BrandshareDamSync.Application.Commands.CreateJob
{
    public sealed record CreateJobCommand(
        string Name,
        string SourceFolderPath,
        string DestinationFolderPath
    ) : IRequest<string>; // returns new Job Id
}
