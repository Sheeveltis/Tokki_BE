using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Alphabet.DTOs;

namespace Tokki.Application.UseCases.Alphabet.Queries
{
    public class GetAlphabetAllQueryHandler : IRequestHandler<GetAlphabetAllQuery, OperationResult<List<AlphabetDto>>>
    {
        private readonly IAlphabetRepository _alphabetRepo;

        public GetAlphabetAllQueryHandler(IAlphabetRepository alphabetRepo)
        {
            _alphabetRepo = alphabetRepo;
        }

        public async Task<OperationResult<List<AlphabetDto>>> Handle(GetAlphabetAllQuery request, CancellationToken cancellationToken)
        {
            var data = await _alphabetRepo.GetAllAsync();

            var query = data.AsQueryable();

            if (request.Type.HasValue)
            {
                query = query.Where(x => x.Type == request.Type.Value);
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive.Value);
            }

            var result = query
                .OrderBy(x => x.SortOrder)
                .Select(x => new AlphabetDto
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

            return OperationResult<List<AlphabetDto>>.Success(result);
        }
    }
}
