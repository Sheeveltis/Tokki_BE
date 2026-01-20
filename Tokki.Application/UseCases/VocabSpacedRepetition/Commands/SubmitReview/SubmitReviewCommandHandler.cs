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
        private readonly IVocabularyRepository _vocabularyRepository;

        public SubmitReviewCommandHandler(IUserVocabProgressRepository repo, IIdGeneratorService idGen, IVocabularyRepository vocabularyRepository)
        {
            _repo = repo;
            _idGen = idGen;
            _vocabularyRepository = vocabularyRepository;
        }

        public async Task<OperationResult<ReviewResponse>> Handle(SubmitReviewCommand request, CancellationToken cancellationToken)
        {
            var progress = await _repo.GetByVocabIdAsync(request.UserId, request.VocabularyId, cancellationToken);
            var vocab = await _vocabularyRepository.GetByIdAsync(request.VocabularyId);
            if(vocab == null)
            {
                return OperationResult<ReviewResponse>.Failure(AppErrors.VocabularyNotFound, 400);
            }

            if (progress == null)
            {
                progress = new UserVocabProgress
                {
                    UserVocabProgressId = _idGen.Generate(15),
                    UserId = request.UserId,
                    VocabularyId = request.VocabularyId,

                    BoxLevel = BoxLevel.Learning,
                    Streak = 0,

                    IsMastered = false,

                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddAsync(progress, cancellationToken);
            }

            CalculateLogic(progress, request.IsCorrect);
            progress.UpdatedAt = DateTime.UtcNow;
            progress.LastReviewedAt = DateTime.UtcNow;
            await _repo.SaveChangesAsync(cancellationToken);

            return OperationResult<ReviewResponse>.Success(new ReviewResponse
            {
                VocabularyId = progress.VocabularyId,
                IsMastered = progress.IsMastered,
            });
        }

        /// <summary>
        /// Logic cốt lõi của hệ thống Leitner (Hộp thẻ nhớ)
        /// </summary>
        private void CalculateLogic(UserVocabProgress p, bool isCorrect)
        {
            if (isCorrect)
            {
                if (p.BoxLevel < BoxLevel.Mastered)
                {
                    p.BoxLevel++;

                    p.Streak = 0;
                }
                else
                {
                    p.Streak++;

                    if (p.Streak >= 2)
                    {
                        p.IsMastered = true;
                    }
                }
            }
            else
            {

                p.Streak = 0;

                if (p.IsMastered)
                {
                    p.IsMastered = false;
                }

                if (p.BoxLevel > BoxLevel.Learning)
                {
                    p.BoxLevel--;
                }
                else
                {
                    p.BoxLevel = BoxLevel.Learning;
                }
            }

            if (p.IsMastered)
            {
                p.IntervalDays = 90;
            }
            else
            {
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