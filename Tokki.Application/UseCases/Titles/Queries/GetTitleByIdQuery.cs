using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Titles.Queries.GetTitleById
{
    public record GetTitleByIdQuery(string Id) : IRequest<OperationResult<Title>>;
}