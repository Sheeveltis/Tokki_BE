using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories; // Sử dụng Interface Repository
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Exam.Commands.SubmitExam
{
    public class SubmitExamCommandHandler : IRequestHandler<SubmitExamCommand, OperationResult<int>>
    {
        private readonly IUserRoadmapRepository _userRoadmapRepository;
        private readonly IUserWeaknessRepository _userWeaknessRepository; // Inject thêm Repository mới
        private readonly IIdGeneratorService _idGenerator;

        public SubmitExamCommandHandler(
            IUserRoadmapRepository userRoadmapRepository,
            IUserWeaknessRepository userWeaknessRepository,
            IIdGeneratorService idGenerator)
        {
            _userRoadmapRepository = userRoadmapRepository;
            _userWeaknessRepository = userWeaknessRepository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<int>> Handle(SubmitExamCommand request, CancellationToken cancellationToken)
        {
            var examQuestions = await _userRoadmapRepository.GetExamQuestionsForGradingAsync(request.ExamId, cancellationToken);

            if (examQuestions == null || !examQuestions.Any())
            {
                return OperationResult<int>.Failure("Đề thi không tồn tại hoặc không có câu hỏi.", 404);
            }

            int totalScore = 0;
            var userExamId = _idGenerator.GenerateCustom(15);
            var examDetails = new List<UserExamDetail>();

            foreach (var userAnswer in request.Answers)
            {
                var questionEntity = examQuestions.FirstOrDefault(q => q.QuestionBankId == userAnswer.QuestionId);
                if (questionEntity == null) continue;

                var correctOption = questionEntity.QuestionBank.QuestionOptions.FirstOrDefault(o => o.IsCorrect);

                bool isCorrect = false;
                if (correctOption != null && correctOption.OptionId == userAnswer.SelectedOptionId)
                {
                    isCorrect = true;
                    totalScore += questionEntity.Score;
                }

                examDetails.Add(new UserExamDetail
                {
                    DetailId = _idGenerator.GenerateCustom(15),
                    UserExamId = userExamId,
                    QuestionId = userAnswer.QuestionId,
                    SelectedOptionId = userAnswer.SelectedOptionId,
                    IsCorrect = isCorrect,
                    QuestionTypeId = questionEntity.QuestionBank.QuestionTypeId ?? "UNKNOWN"
                });
            }

            var userExam = new UserExam
            {
                UserExamId = userExamId,
                UserId = request.UserId,
                ExamId = request.ExamId,
                StartTime = DateTime.UtcNow.AddMinutes(-60),
                SubmitTime = DateTime.UtcNow,
                Score = totalScore,
                Status = 1,
                CreatedAt = DateTime.UtcNow
            };

            await _userRoadmapRepository.AddUserExamAsync(userExam);
            await _userRoadmapRepository.AddUserExamDetailsAsync(examDetails);

            var examResultsByType = examDetails
                .GroupBy(d => d.QuestionTypeId)
                .Select(g => new
                {
                    TypeId = g.Key,
                    Score = (double)g.Count(x => x.IsCorrect) / g.Count() * 100
                })
                .ToList();

            var existingWeaknesses = await _userWeaknessRepository.GetByUserIdAsync(request.UserId, cancellationToken);

            var activeRoadmap = await _userRoadmapRepository.GetActiveRoadmapByUserIdAsync(request.UserId, cancellationToken);
            string? currentRoadmapId = activeRoadmap?.UserRoadmapId;

            foreach (var result in examResultsByType)
            {
                if (result.TypeId == "UNKNOWN") continue;

                bool isWeak = result.Score < 50;
                bool isFixed = result.Score >= 80;

                var weaknessRecord = existingWeaknesses.FirstOrDefault(w => w.QuestionTypeId == result.TypeId);

                if (weaknessRecord != null)
                {
                    weaknessRecord.CurrentScore = result.Score;
                    weaknessRecord.UpdatedAt = DateTime.UtcNow;

                    if (isFixed) weaknessRecord.Status = 2; 
                    else if (result.Score > (weaknessRecord.InitialScore ?? 0)) weaknessRecord.Status = 1;
                    else if (isWeak) weaknessRecord.Status = 0; 
                }
                else
                {
                    if (isWeak)
                    {
                        var newWeakness = new UserWeakness
                        {
                            Id = _idGenerator.GenerateCustom(15),
                            UserId = request.UserId,
                            QuestionTypeId = result.TypeId,
                            RoadmapId = currentRoadmapId,
                            Status = 0,
                            InitialScore = result.Score,
                            CurrentScore = result.Score,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _userWeaknessRepository.AddAsync(newWeakness, cancellationToken);
                    }
                }
            }
            await _userRoadmapRepository.SaveChangesAsync(cancellationToken);
            await _userWeaknessRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<int>.Success(totalScore, 200, "Nộp bài thành công!");
        }
    }
}