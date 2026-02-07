using MediatR;
using Tokki.Application.Common.Models; 
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Titles.Queries.GetAllTitles
{
    public record GetAllTitlesQuery : IRequest<OperationResult<List<Title>>>;
}