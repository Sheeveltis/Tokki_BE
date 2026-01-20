using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.VocabSpacedRepetition.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.VocabSpacedRepetition.Commands.SubmitReview
{
    public class SubmitReviewCommandHandler : IRequestHandler<SubmitReviewCommand, OperationResult<ReviewResponse>>
    {
        private readonly IUserVocabProgressRepository _repo;
        private readonly IIdGeneratorService _idGen;

        public SubmitReviewCommandHandler(IUserVocabProgressRepository repo, IIdGeneratorService idGen)
        {
            _repo = repo;
            _idGen = idGen;
        }

        public async Task<OperationResult<ReviewResponse>> Handle(SubmitReviewCommand request, CancellationToken cancellationToken)
        {
            var progress = await _repo.GetByVocabIdAsync(request.UserId, request.VocabularyId, cancellationToken);

            if (progress != null && progress.IsMastered)
            {
                return OperationResult<ReviewResponse>.Success(new ReviewResponse
                {
                    VocabularyId = request.VocabularyId,
                    IsMastered = true
                });
            }

            if (progress == null)
            {
                progress = new UserVocabProgress
                {
                    UserVocabProgressId = _idGen.Generate(15),
                    UserId = request.UserId,
                    VocabularyId = request.VocabularyId,
                    BoxLevel = BoxLevel.New,
                    Streak = 0,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddAsync(progress, cancellationToken);
            }

            CalculateLogic(progress, request.Quality);

            progress.UpdatedAt = DateTime.UtcNow;
            progress.LastReviewedAt = DateTime.UtcNow;

            await _repo.SaveChangesAsync(cancellationToken);

            return OperationResult<ReviewResponse>.Success(new ReviewResponse
            {
                VocabularyId = progress.VocabularyId,
                IsMastered = progress.IsMastered
            });
        }

        private void CalculateLogic(UserVocabProgress p, QualityVocab quality)
        {
            if (quality >= QualityVocab.Easy)
            {
                if (p.BoxLevel < BoxLevel.Mastered)
                {
                    p.BoxLevel++;

                    if (p.BoxLevel == BoxLevel.Mastered)
                    {
                        p.Streak = 0;       
                        p.IntervalDays = 30; 
                    }
                    else
                    {
                        p.Streak = 0;
                        p.IntervalDays = GetIntervalByLevel(p.BoxLevel);
                    }
                }
                else
                {
                    p.Streak++;
                    p.IntervalDays = 30;

                    if (p.Streak >= 2)
                    {
                        p.IsMastered = true;
                    }
                }
            }
            else
            {
                p.Streak = 0;

                if (p.IsMastered) p.IsMastered = false;

                if (p.BoxLevel > BoxLevel.Learning)
                {
                    p.BoxLevel--;
                }
                else
                {
                    p.BoxLevel = BoxLevel.Learning;
                }

                p.IntervalDays = GetIntervalByLevel(p.BoxLevel);
            }

            if (p.IntervalDays < 1)
                p.NextReviewAt = DateTime.UtcNow.AddMinutes(10);
            else
                p.NextReviewAt = DateTime.UtcNow.AddDays(p.IntervalDays);
        }

        private double GetIntervalByLevel(BoxLevel level)
        {
            return level switch
            {
                BoxLevel.New => 0,
                BoxLevel.Learning => 1,
                BoxLevel.Reviewing => 3,
                BoxLevel.Mastering => 7,
                BoxLevel.Advanced => 14,
                BoxLevel.Mastered => 30,
                _ => 1
            };
        }
    }
}