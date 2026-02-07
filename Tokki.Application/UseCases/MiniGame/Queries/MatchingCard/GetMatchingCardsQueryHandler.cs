using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.UseCases.MiniGame.Queries.MatchingCard
{
    public class GetMatchingCardsQueryHandler : IRequestHandler<GetMatchingCardsQuery, OperationResult<List<MatchingCardDTO>>>
    {
        private readonly IMiniGameRepository _miniGameRepo;

        public GetMatchingCardsQueryHandler(IMiniGameRepository miniGameRepo)
        {
            _miniGameRepo = miniGameRepo;
        }

        public async Task<OperationResult<List<MatchingCardDTO>>> Handle(GetMatchingCardsQuery request, CancellationToken cancellationToken)
        {
            

            var vocabEntities = await _miniGameRepo.GetRandomVocabulariesByTopicAsync(request.TopicId, request.Quantity, cancellationToken);

            if (vocabEntities == null || !vocabEntities.Any())
            {
                return OperationResult<List<MatchingCardDTO>>.Failure(AppErrors.MiniGameMatchingVocabNotFound, 404);
            }

            var result = vocabEntities.Select(v => new MatchingCardDTO
            {
                VocabularyId = v.VocabularyId,
                Text = v.Text,
                Pronunciation = v.Pronunciation,
                Definition = v.Definition,
                ImgURL = v.ImgURL,
                AudioURL = v.AudioURL
            }).ToList();

            return OperationResult<List<MatchingCardDTO>>.Success(result);
        }
    }
}
