using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Titles.Queries.GetTitleById
{
    public record GetTitleByIdQuery(int Id) : IRequest<OperationResult<Title>>;
}