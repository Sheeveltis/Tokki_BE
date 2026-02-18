using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.UseCases.MiniGame.Queries.Wordle
{
    public class GetWordleResultHandler : IRequestHandler<GetWordleResultQuery, OperationResult<WordleResultDTO>>
    {
        private readonly IMiniGameRepository _miniGameRepository;
        private readonly IVocabularyRepository _vocabularyRepository;
        public GetWordleResultHandler(IMiniGameRepository miniGameRepository, IVocabularyRepository vocabularyRepository)
        {
            _miniGameRepository = miniGameRepository;
            _vocabularyRepository = vocabularyRepository;
        }

        public async Task<OperationResult<WordleResultDTO>> Handle(GetWordleResultQuery request, CancellationToken token)
        {
            var progressList = await _miniGameRepository.GetUserWordleProgressAsync(request.UserId, new[] { request.DailyWordleId }, token);
            var progress = progressList.FirstOrDefault();
            if (progress == null || !progress.IsWon)
            {
                return OperationResult<WordleResultDTO>.Failure("Bạn cần hoàn thành và chiến thắng trò chơi để xem kết quả này.");
            }
            var game = await _miniGameRepository.GetDailyWordleByIdAsync(request.DailyWordleId, token);
            if (game == null) return OperationResult<WordleResultDTO>.Failure("Không tìm thấy thông tin trò chơi.");
            var vocab = await _vocabularyRepository.GetByIdAsync(game.VocabularyId);
            var result = new WordleResultDTO
            {
                DailyWordleId = game.DailyWordleId,
                Word = game.Word,
                Definition = vocab.Definition ?? "",
                ImageUrl = vocab.ImgURL ?? "",
                AudioUrl = vocab.AudioURL ?? "",
                AttemptCount = progress.AttemptCount
            };

            return OperationResult<WordleResultDTO>.Success(result);
        }
    }
}
