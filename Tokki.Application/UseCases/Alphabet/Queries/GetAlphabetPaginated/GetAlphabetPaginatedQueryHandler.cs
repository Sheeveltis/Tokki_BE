using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Alphabet.DTOs;

namespace Tokki.Application.UseCases.Alphabet.Queries
{
    public class GetAlphabetPaginatedQueryHandler : IRequestHandler<GetAlphabetPaginatedQuery, OperationResult<PagedResult<AlphabetDto>>>
    {
        private readonly IAlphabetRepository _alphabetRepo;

        public GetAlphabetPaginatedQueryHandler(IAlphabetRepository alphabetRepo)
        {
            _alphabetRepo = alphabetRepo;
        }

        public async Task<OperationResult<PagedResult<AlphabetDto>>> Handle(GetAlphabetPaginatedQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _alphabetRepo.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Type,
                request.IsActive);

            var dtos = items.Select(x => new AlphabetDto
            {
                Id = x.Id,
                Letter = x.Letter,
                Meaning = x.Meaning,
                Pronunciation = x.Pronunciation,
                Type = (int)x.Type,
                TotalStrokes = x.TotalStrokes,
                AudioUrl = x.AudioUrl,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            }).ToList();

            var result = PagedResult<AlphabetDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize);

            return OperationResult<PagedResult<AlphabetDto>>.Success(result);
        }
    }
}
