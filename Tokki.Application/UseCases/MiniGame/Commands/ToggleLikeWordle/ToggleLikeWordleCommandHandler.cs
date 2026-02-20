using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.MiniGame.Commands.ToggleLikeWordle
{
    public class ToggleLikeWordleCommandHandler : IRequestHandler<ToggleLikeWordleCommand, OperationResult<bool>>
    {
        private readonly IMiniGameRepository _repository;
        private readonly IIdGeneratorService _idGenerator;

        public ToggleLikeWordleCommandHandler(IMiniGameRepository repository, IIdGeneratorService idGenerator)
        {
            _repository = repository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<bool>> Handle(ToggleLikeWordleCommand request, CancellationToken token)
        {
            var submission = await _repository.GetWordleSubmissionByIdAsync(request.SubmissionId, token);
            if (submission == null)
                return OperationResult<bool>.Failure("Không tìm thấy bài nộp này.", 404);
            var existingLike = await _repository.GetLikeAsync(request.UserId, request.SubmissionId, token);

            if (existingLike != null)
            {
                _repository.RemoveLike(existingLike);

                submission.LikeCount = Math.Max(0, submission.LikeCount - 1);
            }
            else
            {
                var newLike = new WordleSentenceLike
                {
                    LikeId = _idGenerator.Generate(20),
                    SubmissionId = request.SubmissionId,
                    UserId = request.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                _repository.AddLike(newLike);
                submission.LikeCount++;
            }
            await _repository.SaveChangesAsync(token);

            return OperationResult<bool>.Success(true);
        }
    }
}
