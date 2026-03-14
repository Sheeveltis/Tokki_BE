using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Commands.ProcessWeeklyResult
{
    public class ProcessWeeklyResultCommandHandler
        : IRequestHandler<ProcessWeeklyResultCommand, OperationResult<ProcessWeeklyResultData>>
    {
        private readonly IUserRoadmapRepository _roadmapRepository;
        private readonly IUserWeaknessRepository _weaknessRepository;
        private readonly IRoadmapKnowledgeProfileRepository _knowledgeProfileRepository;
        private readonly IUserExamRepository _userExamRepository;
        private readonly IIdGeneratorService _idGenerator;

        private const double MasteryThreshold = 80.0;
        private const int MaxConsecutiveFail = 2;

        public ProcessWeeklyResultCommandHandler(
            IUserRoadmapRepository roadmapRepository,
            IUserWeaknessRepository weaknessRepository,
            IRoadmapKnowledgeProfileRepository knowledgeProfileRepository,
            IUserExamRepository userExamRepository,
            IIdGeneratorService idGenerator)
        {
            _roadmapRepository = roadmapRepository;
            _weaknessRepository = weaknessRepository;
            _knowledgeProfileRepository = knowledgeProfileRepository;
            _userExamRepository = userExamRepository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<ProcessWeeklyResultData>> Handle(
            ProcessWeeklyResultCommand request,
            CancellationToken cancellationToken)
        {
            var session = await _userExamRepository.GetByIdAsync(
                request.UserExamId, cancellationToken);

            if (session == null)
                return OperationResult<ProcessWeeklyResultData>.Failure(
                    "Không tìm thấy kết quả bài thi.", 404);

            if (session.UserId != request.UserId)
                return OperationResult<ProcessWeeklyResultData>.Failure(
                    "Bạn không có quyền thao tác.", 403);

            if (session.Status != UserExamStatus.Completed)
                return OperationResult<ProcessWeeklyResultData>.Failure(
                    "Bài thi chưa được nộp.", 400);

            var activeRoadmap = await _roadmapRepository
                .GetActiveRoadmapByUserIdAsync(request.UserId, cancellationToken);

            if (activeRoadmap == null)
                return OperationResult<ProcessWeeklyResultData>.Failure(
                    "Không tìm thấy lộ trình đang hoạt động.", 404);

            var examWeek = activeRoadmap.Weeks
                .FirstOrDefault(w => w.WeeklyExamId == session.ExamId);

            if (examWeek == null)
                return OperationResult<ProcessWeeklyResultData>.Failure(
                    "Bài thi này không thuộc lộ trình hiện tại.", 400);

            int currentWeekIndex = examWeek.WeekIndex;

            var templateParts = session.Exam?.ExamTemplate?.TemplateParts
                ?? new List<TemplatePart>();

            double maxScore = templateParts.Sum(p =>
                (p.QuestionTo - p.QuestionFrom + 1) * p.Mark);

            int scorePercent = maxScore > 0
                ? (int)Math.Round(session.Score / maxScore * 100)
                : 0;

            var typeResults = new Dictionary<string, (int Correct, int Total)>();

            foreach (var answer in session.UserExamAnswers)
            {
                var typeId = answer.Question?.QuestionTypeId;
                if (string.IsNullOrEmpty(typeId)) continue;

                if (!typeResults.ContainsKey(typeId))
                    typeResults[typeId] = (0, 0);

                var current = typeResults[typeId];
                typeResults[typeId] = (
                    current.Correct + (answer.IsCorrect == true ? 1 : 0),
                    current.Total + 1
                );
            }

            var weakTypeIds = new List<string>();
            var persistentWeakTypeIds = new List<string>();

            foreach (var (typeId, result) in typeResults)
            {
                double masteryScore = result.Total > 0
                    ? (double)result.Correct / result.Total * 100
                    : 0;

                bool isPassed = masteryScore >= MasteryThreshold;

                if (!isPassed) weakTypeIds.Add(typeId);

                var profile = await _knowledgeProfileRepository
                    .GetAsync(activeRoadmap.UserRoadmapId, typeId, cancellationToken);

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
                else if (currentWeekIndex >= profile.LastEvaluatedWeekIndex)
                {
                    profile.MasteryScore = masteryScore;
                    profile.IsWeakness = !isPassed;
                    profile.LastUpdated = DateTime.UtcNow;
                    profile.LastEvaluatedWeekIndex = currentWeekIndex;
                    profile.ConsecutiveFailWeeks = isPassed
                        ? 0
                        : profile.ConsecutiveFailWeeks + 1;
                }

                if (!isPassed && profile.ConsecutiveFailWeeks >= MaxConsecutiveFail)
                    persistentWeakTypeIds.Add(typeId);
            }

            await _knowledgeProfileRepository.SaveChangesAsync(cancellationToken);

            var existingWeaknesses = await _weaknessRepository
                .GetByUserIdAsync(request.UserId, cancellationToken);

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
                    await _weaknessRepository.AddAsync(new UserWeakness
                    {
                        Id = _idGenerator.GenerateCustom(15),
                        UserId = request.UserId,
                        QuestionTypeId = typeId,
                        RoadmapId = activeRoadmap.UserRoadmapId,
                        Status = 0,
                        InitialScore = score,
                        CurrentScore = score,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }
            }

            await _roadmapRepository.SaveChangesAsync(cancellationToken);
            await _weaknessRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<ProcessWeeklyResultData>.Success(
                new ProcessWeeklyResultData
                {
                    ScorePercent = scorePercent,
                    WeakTypeIds = weakTypeIds,
                    PersistentWeakTypeIds = persistentWeakTypeIds,
                    HasWarning = persistentWeakTypeIds.Any()
                });
        }
    }
}