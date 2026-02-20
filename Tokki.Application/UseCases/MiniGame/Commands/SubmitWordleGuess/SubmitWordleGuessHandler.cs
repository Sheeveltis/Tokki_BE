using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text; 
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.MiniGame.DTOs;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.MiniGame.Commands.SubmitWordleGuess
{
    public class SubmitWordleGuessHandler : IRequestHandler<SubmitWordleGuessCommand, OperationResult<GuessResultDTO>>
    {
        private readonly IMiniGameRepository _miniGameRepository;
        private readonly IIdGeneratorService _idGenerator;

        public SubmitWordleGuessHandler(
            IMiniGameRepository miniGameRepository,
            IIdGeneratorService idGenerator)
        {
            _miniGameRepository = miniGameRepository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<GuessResultDTO>> Handle(SubmitWordleGuessCommand request, CancellationToken token)
        {
            var safeGuessWord = request.GuessWord.Normalize(NormalizationForm.FormC);

            var game = await _miniGameRepository.GetDailyWordleByIdAsync(request.DailyWordleId, token);

            if (game == null)
                return OperationResult<GuessResultDTO>.Failure("Không tìm thấy đề thi này.");

            var safeTargetWord = game.Word.Normalize(NormalizationForm.FormC);

            if (safeGuessWord.Length != safeTargetWord.Length)
                return OperationResult<GuessResultDTO>.Failure($"Độ dài từ không hợp lệ. Yêu cầu: {safeTargetWord.Length} ký tự.");

            var progressList = await _miniGameRepository.GetUserWordleProgressAsync(request.UserId, new[] { request.DailyWordleId }, token);
            var progress = progressList.FirstOrDefault();

            if (progress == null)
            {
                progress = new UserWordleProgress
                {
                    UserWordleProgressId = _idGenerator.Generate(10),
                    UserId = request.UserId,
                    DailyWordleId = request.DailyWordleId,
                    Guesses = new List<string>(),
                    AttemptCount = 0,
                    IsWon = false
                };

                _miniGameRepository.AddUserWordleProgress(progress);
            }

            if (progress.IsWon || progress.AttemptCount >= 6)
                return OperationResult<GuessResultDTO>.Failure("Lượt chơi hôm nay của bạn cho từ này đã kết thúc.");

            var currentGuesses = progress.Guesses;
            currentGuesses.Add(safeGuessWord);
            progress.Guesses = currentGuesses;

            progress.AttemptCount++;
            progress.LastActivity = DateTime.UtcNow; 

            bool isWon = (safeGuessWord == safeTargetWord);
            progress.IsWon = isWon;

            await _miniGameRepository.SaveChangesAsync(token);

            var feedbacks = CalculateFeedback(safeTargetWord, safeGuessWord);

            var resultDto = new GuessResultDTO
            {
                IsWon = isWon,
                IsGameOver = !isWon && progress.AttemptCount >= 6,
                AttemptCount = progress.AttemptCount,
                Feedbacks = feedbacks
            };

            return OperationResult<GuessResultDTO>.Success(resultDto);
        }

        private List<BlockFeedback> CalculateFeedback(string target, string guess)
        {
            var result = new List<BlockFeedback>();

            for (int i = 0; i < target.Length; i++)
            {
                var fb = new BlockFeedback { Character = guess[i] };

                var tParts = HangulHelper.Decompose(target[i]);
                var gParts = HangulHelper.Decompose(guess[i]);

                var tInitials = HangulHelper.GetSubJamos(tParts.Initial);
                var gInitials = HangulHelper.GetSubJamos(gParts.Initial);

                var tVowels = HangulHelper.GetSubJamos(tParts.Vowel);
                var gVowels = HangulHelper.GetSubJamos(gParts.Vowel);

                var tFinals = HangulHelper.GetSubJamos(tParts.Final);
                var gFinals = HangulHelper.GetSubJamos(gParts.Final);

                fb.InitialStatus = (tParts.Initial == gParts.Initial) ? "Green" :
                                   (tInitials.Intersect(gInitials).Any() ? "Yellow" : "Gray");

                fb.VowelStatus = (tParts.Vowel == gParts.Vowel) ? "Green" :
                                 (tVowels.Intersect(gVowels).Any() ? "Yellow" : "Gray");

                fb.FinalStatus = (tParts.Final == gParts.Final) ? "Green" :
                                 (tParts.Final != '\0' && gParts.Final != '\0' && tFinals.Intersect(gFinals).Any() ? "Yellow" : "Gray");

                if (target[i] == guess[i])
                {
                    fb.BlockColor = "Green";
                }
                else if (fb.InitialStatus == "Green" || fb.VowelStatus == "Green" || fb.FinalStatus == "Green" ||
                         fb.InitialStatus == "Yellow" || fb.VowelStatus == "Yellow" || fb.FinalStatus == "Yellow")
                {
                    fb.BlockColor = "Yellow";
                }
                else
                {
                    fb.BlockColor = "Gray";
                }

                result.Add(fb);
            }
            return result;
        }
    }
}