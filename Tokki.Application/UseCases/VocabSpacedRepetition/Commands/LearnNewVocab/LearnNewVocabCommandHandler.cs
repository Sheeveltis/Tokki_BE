using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.VocabSpacedRepetition.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.VocabSpacedRepetition.Commands.LearnNewVocab
{
    public class LearnNewVocabCommandHandler : IRequestHandler<LearnNewVocabCommand, OperationResult<ReviewResponse>>
    {
        private readonly IUserVocabProgressRepository _repo;
        private readonly IIdGeneratorService _idGen;

        public LearnNewVocabCommandHandler(IUserVocabProgressRepository repo, IIdGeneratorService idGen)
        {
            _repo = repo;
            _idGen = idGen;
        }

        public async Task<OperationResult<ReviewResponse>> Handle(LearnNewVocabCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra xem user đã từng học từ này chưa
            var existingProgress = await _repo.GetByVocabIdAsync(request.UserId, request.VocabularyId, cancellationToken);

            if (existingProgress != null)
            {
                return OperationResult<ReviewResponse>.Success(new ReviewResponse
                {
                    VocabularyId = existingProgress.VocabularyId,
                    IsMastered = existingProgress.IsMastered,
                }, 200, "Từ vựng này đã được lưu từ trước.");
            }

            var progress = new UserVocabProgress
            {
                UserVocabProgressId = _idGen.Generate(15),
                UserId = request.UserId,
                VocabularyId = request.VocabularyId,
                BoxLevel = BoxLevel.Learning,
                Streak = 0,
                IntervalDays = 1,
                NextReviewAt = DateTime.UtcNow.AddDays(1),

                LastReviewedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsMastered = false
            };

            await _repo.AddAsync(progress, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            return OperationResult<ReviewResponse>.Success(new ReviewResponse
            {
                VocabularyId = progress.VocabularyId,
                IsMastered = false,
            }, 200, "Đã lưu từ vựng");
        }
    }
}
