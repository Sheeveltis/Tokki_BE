using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using UserExamEntity = Tokki.Domain.Entities.UserExam;

namespace Tokki.Application.UseCases.Exam.Commands.SubmitExam
{
    public class SubmitExamCommandHandler : IRequestHandler<SubmitExamCommand, OperationResult<int>>
    {
        private readonly IUserRoadmapRepository _userRoadmapRepository;
        private readonly IUserWeaknessRepository _userWeaknessRepository;
        private readonly IRoadmapKnowledgeProfileRepository _knowledgeProfileRepository;
        private readonly IIdGeneratorService _idGenerator;

        private const double MasteryThreshold = 80.0;

        public SubmitExamCommandHandler(
            IUserRoadmapRepository userRoadmapRepository,
            IUserWeaknessRepository userWeaknessRepository,
            IRoadmapKnowledgeProfileRepository knowledgeProfileRepository, 
            IIdGeneratorService idGenerator)
        {
            _userRoadmapRepository = userRoadmapRepository;
            _userWeaknessRepository = userWeaknessRepository;
            _knowledgeProfileRepository = knowledgeProfileRepository; 
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<int>> Handle(
            SubmitExamCommand request,
            CancellationToken cancellationToken)
        {
            var examQuestions = await _userRoadmapRepository
                .GetExamQuestionsForGradingAsync(request.ExamId, cancellationToken);

            if (examQuestions == null || !examQuestions.Any())
                return OperationResult<int>.Failure("Đề thi không tồn tại hoặc không có câu hỏi.", 404);

            int totalScore = 0;
            var userExamId = _idGenerator.GenerateCustom(15);
            var examAnswers = new List<UserExamAnswer>();

            var typeResults = new Dictionary<string, (int Correct, int Total)>();

            foreach (var userAnswer in request.Answers)
            {
                var questionEntity = examQuestions
                    .FirstOrDefault(q => q.QuestionBankId == userAnswer.QuestionId);
                if (questionEntity == null) continue;

                var correctOption = questionEntity.QuestionBank.QuestionOptions
                    .FirstOrDefault(o => o.IsCorrect);

                bool isCorrect = correctOption != null
                    && correctOption.OptionId == userAnswer.SelectedOptionId;

                if (isCorrect) totalScore += questionEntity.Score;

                examAnswers.Add(new UserExamAnswer
                {
                    UserExamAnswerId = _idGenerator.GenerateCustom(15),
                    UserExamId = userExamId,
                    QuestionId = userAnswer.QuestionId,
                    SelectedOptionId = userAnswer.SelectedOptionId,
                    IsCorrect = isCorrect,
                    OrderIndex = 0
                });

                var typeId = questionEntity.QuestionBank.QuestionTypeId ?? "UNKNOWN";
                if (typeId != "UNKNOWN")
                {
                    if (!typeResults.ContainsKey(typeId))
                        typeResults[typeId] = (0, 0);

                    var current = typeResults[typeId];
                    typeResults[typeId] = (
                        current.Correct + (isCorrect ? 1 : 0),
                        current.Total + 1
                    );
                }
            }

            var userExam = new UserExamEntity
            {
                UserExamId = userExamId,
                UserId = request.UserId,
                ExamId = request.ExamId,
                StartTime = DateTime.UtcNow.AddMinutes(-60),
                SubmitTime = DateTime.UtcNow,
                Score = totalScore,
                Status = UserExamStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            await _userRoadmapRepository.AddUserExamAsync(userExam);
            await _userRoadmapRepository.AddUserExamAnswersAsync(examAnswers);
           
            var activeRoadmap = await _userRoadmapRepository
                .GetActiveRoadmapByUserIdAsync(request.UserId, cancellationToken);

            if (activeRoadmap != null)
            {
                var examWeek = activeRoadmap.Weeks
                    .FirstOrDefault(w => w.WeeklyExamId == request.ExamId);

                int currentWeekIndex = examWeek?.WeekIndex ?? 0;

                foreach (var (typeId, result) in typeResults)
                {
                    double masteryScore = result.Total > 0
                        ? (double)result.Correct / result.Total * 100
                        : 0;

                    bool isPassed = masteryScore >= MasteryThreshold;

                    var profile = await _knowledgeProfileRepository.GetAsync(
                        activeRoadmap.UserRoadmapId, typeId, cancellationToken);

                    if (profile == null)
                    {
                        profile = new RoadmapKnowledgeProfile
                        {
                            ProfileId = _idGenerator.GenerateCustom(15),
                            UserRoadmapId = activeRoadmap.UserRoadmapId,
                            QuestionTypeId = typeId,
                            MasteryScore = masteryScore,
                            IsWeakness = !isPassed,
                            LastUpdated = DateTime.UtcNow,
                            LastEvaluatedWeekIndex = currentWeekIndex,
                            ConsecutiveFailWeeks = isPassed ? 0 : 1
                        };
                        await _knowledgeProfileRepository.AddAsync(profile, cancellationToken);
                    }
                    else
                    {                     
                        if (currentWeekIndex >= profile.LastEvaluatedWeekIndex)
                        {
                            profile.MasteryScore = masteryScore;
                            profile.IsWeakness = !isPassed;
                            profile.LastUpdated = DateTime.UtcNow;
                            profile.LastEvaluatedWeekIndex = currentWeekIndex;

                            if (isPassed)
                            {
                                profile.ConsecutiveFailWeeks = 0;
                            }
                            else
                            {
                                profile.ConsecutiveFailWeeks += 1;
                            }
                        }
                    }
                }

                await _knowledgeProfileRepository.SaveChangesAsync(cancellationToken);
            }

            var existingWeaknesses = await _userWeaknessRepository
                .GetByUserIdAsync(request.UserId, cancellationToken);

            string? currentRoadmapId = activeRoadmap?.UserRoadmapId;

            foreach (var (typeId, result) in typeResults)
            {
                double score = result.Total > 0
                    ? (double)result.Correct / result.Total * 100
                    : 0;

                bool isWeak = score < 50;
                bool isFixed = score >= 80;

                var weaknessRecord = existingWeaknesses
                    .FirstOrDefault(w => w.QuestionTypeId == typeId);

                if (weaknessRecord != null)
                {
                    weaknessRecord.CurrentScore = score;
                    weaknessRecord.UpdatedAt = DateTime.UtcNow;

                    if (isFixed) weaknessRecord.Status = 2;
                    else if (score > (weaknessRecord.InitialScore ?? 0)) weaknessRecord.Status = 1;
                    else if (isWeak) weaknessRecord.Status = 0;
                }
                else if (isWeak)
                {
                    await _userWeaknessRepository.AddAsync(new UserWeakness
                    {
                        Id = _idGenerator.GenerateCustom(15),
                        UserId = request.UserId,
                        QuestionTypeId = typeId,
                        RoadmapId = currentRoadmapId,
                        Status = 0,
                        InitialScore = score,
                        CurrentScore = score,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }
            }
            await _userRoadmapRepository.SaveChangesAsync(cancellationToken);
            await _userWeaknessRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<int>.Success(totalScore, 200, "Nộp bài thành công!");
        }
    }
}