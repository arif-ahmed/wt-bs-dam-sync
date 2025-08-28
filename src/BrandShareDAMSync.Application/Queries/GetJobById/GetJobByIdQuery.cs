using System;
using MediatR;
using BrandshareDamSync.Application.Models;

namespace BrandshareDamSync.Application.Queries.GetJobById
{
    public sealed record GetJobByIdQuery(string Id) : IRequest<JobDto?>;
}
