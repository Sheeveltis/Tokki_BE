using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.UseCases.MiniGame.Queries.Wordle
{
    public class GetWordleLeaderboardQueryHandler : IRequestHandler<GetWordleLeaderboardQuery, OperationResult<List<WordleSentenceDto>>>
    {
        private readonly IMiniGameRepository _repository;

        public GetWordleLeaderboardQueryHandler(IMiniGameRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<List<WordleSentenceDto>>> Handle(GetWordleLeaderboardQuery request, CancellationToken token)
        {
            var submissions = await _repository.GetWordleLeaderboardAsync(request.DailyWordleId, token, request.IncludePrivate);

            var result = submissions.Select(s =>
            {
                var currentTitle = s.User?.CurrentTitle;

                var dto = new WordleSentenceDto
                {
                    SubmissionId = s.SubmissionId,
                    CreatedAt = s.CreatedAt,
                    SentenceContent = s.SentenceContent,
                    AiScore = s.AiScore,
                    LikeCount = s.LikeCount,
                    IsPublic = s.IsPublic,
                    IsAnonymous = s.IsAnonymous,
                    UserId = s.UserId,
                    UserName = s.User?.FullName ?? "Học viên Tokki",
                    AvatarUrl = s.User?.AvatarUrl,
                    TitleName = currentTitle?.Name,
                    TitleColorHex = currentTitle?.ColorHex,
                    TitleIconUrl = currentTitle?.IconUrl
                };

                return dto;
            }).ToList();

            return OperationResult<List<WordleSentenceDto>>.Success(result);
        }
    }
}
