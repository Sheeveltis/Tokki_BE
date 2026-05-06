using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.MiniGame.Queries.Wordle
{
    public class GetWordlePaginatedQueryHandler : IRequestHandler<GetWordlePaginatedQuery, OperationResult<PagedResult<WordleAdminDto>>>
    {
        private readonly IWordleRepository _wordleRepo;

        public GetWordlePaginatedQueryHandler(IWordleRepository wordleRepo)
        {
            _wordleRepo = wordleRepo;
        }

        public async Task<OperationResult<PagedResult<WordleAdminDto>>> Handle(GetWordlePaginatedQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _wordleRepo.GetPagedDailyWordlesAsync(
                request.PageNumber,
                request.PageSize,
                request.Date,
                request.Level,
                request.SearchTerm,
                cancellationToken);

            var dtos = items.Select(x => new WordleAdminDto
            {
                DailyWordleId = x.Item.DailyWordleId,
                GameDate = x.Item.GameDate,
                Level = x.Item.Level,
                Word = x.Item.Word,
                VocabularyId = x.Item.VocabularyId,
                Definition = x.Item.Vocabulary != null ? x.Item.Vocabulary.Definition : null,
                Pronunciation = x.Item.Vocabulary != null ? x.Item.Vocabulary.Pronunciation : null,
                IsLocked = x.IsLocked
            }).ToList();

            var result = PagedResult<WordleAdminDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize);

            return OperationResult<PagedResult<WordleAdminDto>>.Success(result);
        }
    }
}
