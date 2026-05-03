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
        private readonly ISystemConfigRepository _systemConfigRepository;

        public GetDailyWordleStatusHandler(
            IMiniGameRepository miniGameRepository,
            ISystemConfigRepository systemConfigRepository)
        {
            _miniGameRepository = miniGameRepository;
            _systemConfigRepository = systemConfigRepository;
        }

        public async Task<OperationResult<WordleDashboardDTO>> Handle(GetDailyWordleStatusQuery request, CancellationToken token)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var dailyGames = await _miniGameRepository.GetDailyWordlesByDateAsync(today, token);

            var maxAttemptsStr = await _systemConfigRepository.GetValueByKeyAsync("WORDLE_MAX_ATTEMPTS");
            int maxAttempts = int.TryParse(maxAttemptsStr, out var val) ? val : 6;

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
                    MaxAttempts = maxAttempts,
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