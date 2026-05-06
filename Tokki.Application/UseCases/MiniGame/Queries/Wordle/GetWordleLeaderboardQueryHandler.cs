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
    public class GetWordleLeaderboardQueryHandler : IRequestHandler<GetWordleLeaderboardQuery, OperationResult<PagedResult<WordleSentenceDto>>>
    {
        private readonly IMiniGameRepository _repository;

        public GetWordleLeaderboardQueryHandler(IMiniGameRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<PagedResult<WordleSentenceDto>>> Handle(GetWordleLeaderboardQuery request, CancellationToken token)
        {
            var (submissions, totalCount) = await _repository.GetWordleLeaderboardAsync(request.DailyWordleId, request.PageIndex, request.PageSize, token, request.IncludePrivate);

            var items = submissions.Select(s =>
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

            var pagedResult = PagedResult<WordleSentenceDto>.Create(items, totalCount, request.PageIndex, request.PageSize);

            return OperationResult<PagedResult<WordleSentenceDto>>.Success(pagedResult);
        }
    }
}
