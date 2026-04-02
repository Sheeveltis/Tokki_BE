using MediatR;
using System.Net; 
using Tokki.Application.Common.Models; 
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.DTOs;
using Tokki.Application.Common.Helpers;

namespace Tokki.Application.UseCases.MiniGame.Queries.Wordle
{
    public class GetDailyWordleStatusHandler : IRequestHandler<GetDailyWordleStatusQuery, OperationResult<WordleDashboardDTO>>
    {
        private readonly IMiniGameRepository _miniGameRepository;

        public GetDailyWordleStatusHandler(IMiniGameRepository miniGameRepository)
        {
            _miniGameRepository = miniGameRepository;
        }

        public async Task<OperationResult<WordleDashboardDTO>> Handle(GetDailyWordleStatusQuery request, CancellationToken token)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var dailyGames = await _miniGameRepository.GetDailyWordlesByDateAsync(today, token);

            var dashboard = new WordleDashboardDTO
            {
                Date = today,
                Levels = new List<WordleLevelStatus>()
            };

            if (!dailyGames.Any())
            {
                return OperationResult<WordleDashboardDTO>.Success(dashboard);
            }

            var gameIds = dailyGames.Select(g => g.DailyWordleId).ToList();
            var progresses = await _miniGameRepository.GetUserWordleProgressAsync(request.UserId, gameIds, token);

            foreach (var game in dailyGames)
            {
                var userProgress = progresses.FirstOrDefault(p => p.DailyWordleId == game.DailyWordleId);

                dashboard.Levels.Add(new WordleLevelStatus
                {
                    DailyWordleId = game.DailyWordleId,
                    Level = game.Level,
                    WordLength = game.Word.Length,

                    IsWon = userProgress?.IsWon ?? false,
                    AttemptCount = userProgress?.AttemptCount ?? 0,
                    MaxAttempts = 6,
                    Attempts = (userProgress?.Guesses ?? new List<string>())
                                        .Select(g => new WordleAttemptDTO
                                        {
                                            Guess = g,
                                            Feedbacks = WordleHelper.CalculateFeedback(game.Word, g)
                                        })
                                        .ToList()
                });
            }

            return OperationResult<WordleDashboardDTO>.Success(dashboard);
        }
    }
}