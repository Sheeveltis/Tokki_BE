using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.MiniGame.Queries.Wordle
{
    public class GetSuitableVocabsQueryHandler : IRequestHandler<GetSuitableVocabsQuery, OperationResult<PagedResult<VocabularyDto>>>
    {
        private readonly IWordleRepository _wordleRepo;

        public GetSuitableVocabsQueryHandler(IWordleRepository wordleRepo)
        {
            _wordleRepo = wordleRepo;
        }

        public async Task<OperationResult<PagedResult<VocabularyDto>>> Handle(GetSuitableVocabsQuery request, CancellationToken cancellationToken)
        {
            int length = request.Level switch
            {
                WordleLevel.Easy => 2,
                WordleLevel.Medium => 3,
                WordleLevel.Hard => 4,
                _ => 3
            };

            var (items, totalCount) = await _wordleRepo.GetPagedSuitableVocabsAsync(
                length,
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                cancellationToken);

            var dtos = items.Select(v => new VocabularyDto
            {
                VocabularyId = v.VocabularyId,
                Text = v.Text,
                Definition = v.Definition,
                Pronunciation = v.Pronunciation,
                ImgURL = v.ImgURL,
                Status = v.Status,
                CreateDate = v.CreateDate,
                CreateBy = v.CreateBy
            }).ToList();

            var result = PagedResult<VocabularyDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize);

            return OperationResult<PagedResult<VocabularyDto>>.Success(result);
        }
    }
}
