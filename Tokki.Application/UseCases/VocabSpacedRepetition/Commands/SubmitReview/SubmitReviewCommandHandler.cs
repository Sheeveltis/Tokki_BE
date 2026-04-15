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
                // TRƯỜNG HỢP 1: TỪ MỚI (CHƯA CÓ TRONG TIẾN TRÌNH)
                // Theo yêu cầu: "từ mới thì nó sẽ set box level là learning"
                progress = new UserVocabProgress
                {
                    UserVocabProgressId = _idGen.Generate(15),
                    UserId = request.UserId,
                    VocabularyId = request.VocabularyId,

                    BoxLevel = BoxLevel.Learning, // Bắt đầu ở Learning (Box 1)
                    Streak = request.IsCorrect ? 1 : 0,

                    IsMastered = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastReviewedAt = DateTime.UtcNow
                };

                // Nếu đúng ngay lần đầu, ôn lại sau 1 ngày (Hũ 1). 
                // Nếu sai, ôn lại ngay sau 10 phút.
                progress.IntervalDays = request.IsCorrect ? GetIntervalByLevel(BoxLevel.Learning) : 0;
                
                if (progress.IntervalDays < 1)
                    progress.NextReviewAt = DateTime.UtcNow.AddMinutes(10);
                else
                    progress.NextReviewAt = DateTime.UtcNow.AddDays(progress.IntervalDays);

                await _repo.AddAsync(progress, cancellationToken);
            }
            else
            {
                // TRƯỜNG HỢP 2: ĐÃ HỌC (NÂNG CẤP HOẶC GIẢM CẤP)
                CalculateLogic(progress, request.IsCorrect);
                
                progress.UpdatedAt = DateTime.UtcNow;
                progress.LastReviewedAt = DateTime.UtcNow;

                // Tính toán ngày ôn tiếp theo dựa trên IntervalDays mới
                if (progress.IntervalDays < 1)
                    progress.NextReviewAt = DateTime.UtcNow.AddMinutes(10);
                else
                    progress.NextReviewAt = DateTime.UtcNow.AddDays(progress.IntervalDays);
            }
            
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
                // Nâng cấp level dần dần
                if (p.BoxLevel < BoxLevel.Mastered)
                {
                    p.BoxLevel++;
                    p.Streak = 0; // Reset streak khi lên level mới
                }
                else
                {
                    // Nếu đã ở level cao nhất, tăng streak để đạt trạng thái "Mastered" hoàn toàn
                    p.Streak++;

                    if (p.Streak >= 2)
                    {
                        p.IsMastered = true;
                    }
                }

                // Cập nhật IntervalDays theo level mới
                if (p.IsMastered)
                {
                    p.IntervalDays = 90; // Thuộc lòng rồi thì 3 tháng sau mới xem lại
                }
                else
                {
                    p.IntervalDays = GetIntervalByLevel(p.BoxLevel);
                }
            }
            else
            {
                // GIẢM CÁC THỨ ĐỒ KHI SAI
                p.Streak = 0;
                p.IsMastered = false;

                // Giảm level xuống 1 bậc, tối thiểu là Learning
                if (p.BoxLevel > BoxLevel.Learning)
                {
                    p.BoxLevel--;
                }
                else
                {
                    p.BoxLevel = BoxLevel.Learning;
                }

                // Sai thì phải làm lại sớm (10 phút sau)
                p.IntervalDays = 0;
            }
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