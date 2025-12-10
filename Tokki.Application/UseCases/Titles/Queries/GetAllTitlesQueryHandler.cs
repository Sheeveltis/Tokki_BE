using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Titles.Queries.GetAllTitles
{
    public class GetAllTitlesQueryHandler : IRequestHandler<GetAllTitlesQuery, OperationResult<List<Title>>>
    {
        private readonly ITitleRepository _titleRepository;

        public GetAllTitlesQueryHandler(ITitleRepository titleRepository)
        {
            _titleRepository = titleRepository;
        }

        public async Task<OperationResult<List<Title>>> Handle(GetAllTitlesQuery request, CancellationToken cancellationToken)
        {
            var titles = await _titleRepository.GetAllTitlesAsync();
            return OperationResult<List<Title>>.Success(titles, 200);
        }
    }
}