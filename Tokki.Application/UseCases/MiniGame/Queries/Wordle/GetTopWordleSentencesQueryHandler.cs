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
    public class GetTopWordleSentencesQueryHandler : IRequestHandler<GetTopWordleSentencesQuery, OperationResult<List<WordleSentenceDto>>>
    {
        private readonly IMiniGameRepository _repository;

        public GetTopWordleSentencesQueryHandler(IMiniGameRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<List<WordleSentenceDto>>> Handle(GetTopWordleSentencesQuery request, CancellationToken token)
        {
            var submissions = await _repository.GetTopPublicSentencesAsync(request.DailyWordleId, request.Top, token);

            if (submissions == null || !submissions.Any())
            {
                return OperationResult<List<WordleSentenceDto>>.Success(new List<WordleSentenceDto>());
            }

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

                    IsLiked = !string.IsNullOrEmpty(request.CurrentUserId) &&
                              s.SentenceLikes.Any(l => l.UserId == request.CurrentUserId)
                };

                if (s.IsAnonymous)
                {
                    dto.UserId = null;
                    dto.UserName = "Người dùng ẩn danh";
                    dto.AvatarUrl = null;
                    dto.TitleName = null;
                    dto.TitleColorHex = null;
                    dto.TitleIconUrl = null;
                }
                else
                {
                    dto.UserId = s.UserId;
                    dto.UserName = s.User?.FullName ?? "Học viên Tokki";
                    dto.AvatarUrl = s.User?.AvatarUrl;

                    dto.TitleName = currentTitle?.Name;
                    dto.TitleColorHex = currentTitle?.ColorHex;
                    dto.TitleIconUrl = currentTitle?.IconUrl;
                }

                return dto;
            }).ToList();

            return OperationResult<List<WordleSentenceDto>>.Success(result);
        }
    }
}