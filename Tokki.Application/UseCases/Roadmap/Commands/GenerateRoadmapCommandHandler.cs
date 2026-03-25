using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Application.UseCases.UserExam.Queries.GetUserExamResult;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap
{
    public class GenerateRoadmapCommandHandler : IRequestHandler<GenerateRoadmapCommand, OperationResult<string>>
    {
        private readonly IAiRoadmapService _aiRoadmapService;
        private readonly IExamAssemblyService _examAssemblyService;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<GenerateRoadmapCommandHandler> _logger;
        private readonly IUserRoadmapRepository _userRoadmapRepository;
        private readonly IUserWeaknessRepository _userWeaknessRepository;
        private readonly IUserExamRepository _userExamRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IRoadmapProgressService _progressService;
        private readonly IServiceScopeFactory _scopeFactory; 
        private readonly IMediator _mediator;
        public GenerateRoadmapCommandHandler(
            IAiRoadmapService aiRoadmapService,
            IExamAssemblyService examAssemblyService,
            IIdGeneratorService idGeneratorService,
            IUserRoadmapRepository userRoadmapRepository,
            IUserWeaknessRepository userWeaknessRepository,
            IUserExamRepository userExamRepository,
            IAccountRepository accountRepository,
            IRoadmapProgressService progressService,
            IServiceScopeFactory scopeFactory,            
            IMediator mediator,
            ILogger<GenerateRoadmapCommandHandler> logger)
        {
            _aiRoadmapService = aiRoadmapService;
            _examAssemblyService = examAssemblyService;
            _idGeneratorService = idGeneratorService;
            _userRoadmapRepository = userRoadmapRepository;
            _userWeaknessRepository = userWeaknessRepository;
            _userExamRepository = userExamRepository;
            _accountRepository = accountRepository;
            _progressService = progressService;
            _scopeFactory = scopeFactory;                
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(GenerateRoadmapCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bắt đầu tạo lộ trình {Days} ngày cho User: {UserId}",
                request.DurationDays, request.UserId);

            var activeRoadmap = await _userRoadmapRepository
                .GetActiveRoadmapByUserIdAsync(request.UserId, cancellationToken);

            if (activeRoadmap != null)
                return OperationResult<string>.Failure(
                    "Bạn đang có một lộ trình học đang hoạt động. Vui lòng hoàn thành hoặc hủy lộ trình cũ trước khi tạo mới.", 400);

            var jobId = _idGeneratorService.GenerateCustom(15);
            _progressService.Set(jobId, new RoadmapProgressState
            {
                JobId = jobId,
                Percent = 0,
                Step = "Đang khởi động..."
            });

            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var sp = scope.ServiceProvider;

                var aiRoadmapService = sp.GetRequiredService<IAiRoadmapService>();
                var examAssemblyService = sp.GetRequiredService<IExamAssemblyService>();
                var idGenerator = sp.GetRequiredService<IIdGeneratorService>();
                var roadmapRepo = sp.GetRequiredService<IUserRoadmapRepository>();
                var weaknessRepo = sp.GetRequiredService<IUserWeaknessRepository>();
                var userExamRepo = sp.GetRequiredService<IUserExamRepository>();
                var accountRepo = sp.GetRequiredService<IAccountRepository>();
                var mediator = sp.GetRequiredService<IMediator>();

                try
                {
                    Report(jobId, 20, "Phân tích kết quả bài test đầu vào...");

                    var currentLevel = CurrentTopikLevel.Pre_Topik;

                    if (!string.IsNullOrEmpty(request.UserExamId))
                    {
                        var examResult = await mediator.Send(
                            new GetUserExamResultQuery { UserExamId = request.UserExamId },
                            CancellationToken.None);

                        if (examResult.IsSuccess && examResult.Data != null)
                        {
                            var skillData = examResult.Data;

                            var calculatedLevel = CalculateLevel(
                                request.TargetAim,
                                skillData.Listening.Score,
                                skillData.Reading.Score,
                                skillData.Writing.Score);

                            var selfDeclaredLevel = await userExamRepo
                                .GetSelfDeclaredLevelAsync(request.UserExamId, CancellationToken.None);

                            currentLevel = selfDeclaredLevel != null
                                ? (CurrentTopikLevel)Math.Min((int)selfDeclaredLevel.Value, (int)calculatedLevel)
                                : calculatedLevel;
                        }
                    }

                    Report(jobId, 35, "Xây dựng chương trình học phù hợp...");

                    var weaknesses = new List<string>();

                    if (!string.IsNullOrEmpty(request.UserExamId))
                    {
                        var questionTypes = await userExamRepo
                            .GetIncorrectQuestionTypesByExamIdAsync(
                                request.UserExamId, CancellationToken.None);

                        weaknesses = questionTypes
                            .Select(qt => qt.QuestionTypeId)
                            .Distinct()
                            .ToList();
                    }

                    if (weaknesses.Any())
                    {
                        var validIds = await roadmapRepo
                            .GetValidQuestionTypeIdsAsync(weaknesses, CancellationToken.None);

                        var invalidIds = weaknesses.Except(validIds).ToList();
                        if (invalidIds.Any())
                            _logger.LogWarning("Loại bỏ {Count} questionTypeId không hợp lệ: {Ids}",
                                invalidIds.Count, string.Join(", ", invalidIds));

                        weaknesses = validIds;
                    }
                    int totalTypes = weaknesses.Count;
                    int totalWeeks = (int)Math.Ceiling((double)request.DurationDays / 7);
                    int typesPerWeek = totalTypes == 0
                        ? 3
                        : Math.Clamp((int)Math.Ceiling((double)totalTypes / totalWeeks), 1, 5);

                    var week1Weaknesses = weaknesses.Take(typesPerWeek).ToList();

                    var weakTypeInfos = week1Weaknesses.Any()
                        ? await roadmapRepo.GetQuestionTypeMenuAsync(
                            week1Weaknesses, CancellationToken.None)
                        : new List<QuestionTypeMenuItem>();

                    var allLevelTypeIds = await roadmapRepo
                        .GetValidQuestionTypeIdsByLevelAsync(currentLevel, CancellationToken.None);

                    var questionTypeMenu = await roadmapRepo
                        .GetQuestionTypeMenuAsync(allLevelTypeIds, CancellationToken.None);

                    Report(jobId, 55, "AI đang tạo nội dung học tập (có thể mất 15-30 giây)...");

                    var aiPlan = await aiRoadmapService.GenerateStudyPlanAsync(
                        request.TargetAim,
                        currentLevel,
                        7,
                        week1Weaknesses,
                        weakTypeInfos,
                        questionTypeMenu,
                        typesPerWeek,
                        totalWeeks);

                    if (aiPlan == null || aiPlan.Weeks == null || !aiPlan.Weeks.Any())
                    {
                        _progressService.Set(jobId, new RoadmapProgressState
                        {
                            JobId = jobId,
                            IsError = true,
                            Step = "AI không thể tạo lộ trình. Vui lòng thử lại.",
                            ErrorMessage = "AI không thể tạo lộ trình. Vui lòng thử lại."
                        });
                        return;
                    }

                    Report(jobId, 75, "Lưu lộ trình vào hệ thống...");

                    var roadmapId = idGenerator.GenerateCustom(15);
                    var roadmap = new UserRoadmap
                    {
                        UserRoadmapId = roadmapId,
                        UserId = request.UserId,
                        TargetAim = request.TargetAim,
                        CurrentLevel = currentLevel,
                        DurationDays = request.DurationDays,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(request.DurationDays),
                        CurrentStatus = UserRoadmapStatus.Active,
                        OverallAiAssessment = aiPlan.Assessment,
                        CreatedAt = DateTime.UtcNow,
                        Weeks = new List<RoadmapWeek>()
                    };

                    for (int i = 1; i <= totalWeeks; i++)
                    {
                        var weekId = idGenerator.GenerateCustom(15);
                        var weekEntity = new RoadmapWeek
                        {
                            RoadmapWeekId = weekId,
                            UserRoadmapId = roadmapId,
                            WeekIndex = i,
                            FromDate = roadmap.StartDate.AddDays((i - 1) * 7),
                            ToDate = roadmap.StartDate.AddDays(i * 7),
                            DailyTasks = new List<RoadmapDailyTask>()
                        };

                        if (i == 1)
                        {
                            weekEntity.Status = RoadmapWeekStatus.InProgress;
                            var weekDto = aiPlan.Weeks.FirstOrDefault();

                            if (weekDto != null)
                            {
                                weekEntity.WeekFocusGoal = weekDto.WeekGoal;

                                var weeklyScope = weekDto.Days
                                    .SelectMany(d => d.Tasks)
                                    .Where(t => t.TaskType == "VirtualQuiz"
                                             && !string.IsNullOrEmpty(t.QuestionTypeId))
                                    .Select(t => t.QuestionTypeId)
                                    .Distinct()
                                    .ToList();

                                foreach (var dayDto in weekDto.Days)
                                {
                                    foreach (var taskDto in dayDto.Tasks)
                                    {
                                        var taskId = idGenerator.GenerateCustom(15);
                                        var taskEntity = new RoadmapDailyTask
                                        {
                                            TaskId = taskId,
                                            RoadmapWeekId = weekId,
                                            DayIndex = dayDto.DayIndex,
                                            Title = taskDto.Title,
                                            AiGeneratedContent = taskDto.Content,
                                            IsCompleted = false
                                        };

                                        if (taskDto.TaskType == "LearnTheory")
                                        {
                                            taskEntity.TaskType = RoadmapTaskType.LearnTheory;
                                            if (!string.IsNullOrEmpty(taskDto.QuestionTypeId))
                                                taskEntity.QuestionTypeId = taskDto.QuestionTypeId;
                                        }
                                        else if (taskDto.TaskType == "VirtualQuiz")
                                        {
                                            taskEntity.TaskType = RoadmapTaskType.VirtualQuiz;
                                            taskEntity.QuestionTypeId = taskDto.QuestionTypeId;
                                        }
                                        else if (taskDto.TaskType == "WeeklyExam")
                                        {
                                            taskEntity.TaskType = RoadmapTaskType.WeeklyExam;

                                            if (weeklyScope.Any())
                                            {
                                                var examType = (request.TargetAim == TargetAimLevel.Topik_I_Level1
                                                    || request.TargetAim == TargetAimLevel.Topik_I_Level2)
                                                    ? ExamType.TopikI
                                                    : ExamType.TopikII;

                                                var examResult = await examAssemblyService
                                                    .GenerateWeeklyExamFromScopeAsync(
                                                        request.UserId,
                                                        i,
                                                        weeklyScope,
                                                        examType,
                                                        CancellationToken.None);

                                                if (examResult.IsSuccess)
                                                {
                                                    taskEntity.ExamId = examResult.Data;
                                                    weekEntity.WeeklyExamId = examResult.Data;
                                                }
                                            }
                                        }

                                        weekEntity.DailyTasks.Add(taskEntity);
                                    }
                                }
                            }
                        }
                        else
                        {
                            weekEntity.Status = RoadmapWeekStatus.Locked;
                            weekEntity.WeekFocusGoal = "Nội dung sẽ được cập nhật dựa trên kết quả tuần trước.";
                        }

                        roadmap.Weeks.Add(weekEntity);
                    }

                    Report(jobId, 90, "Tạo đề thi tuần 1 và cập nhật hồ sơ học viên...");

                    await roadmapRepo.AddAsync(roadmap);
                    await roadmapRepo.SaveChangesAsync(CancellationToken.None);

                    if (weaknesses.Any())
                    {
                        foreach (var weakTypeId in weaknesses)
                        {
                            await weaknessRepo.AddAsync(new UserWeakness
                            {
                                Id = idGenerator.GenerateCustom(15),
                                UserId = request.UserId,
                                RoadmapId = roadmapId,
                                QuestionTypeId = weakTypeId,
                                Status = 0,
                                InitialScore = 0,
                                CurrentScore = 0,
                                CreatedAt = DateTime.UtcNow
                            }, CancellationToken.None);
                        }
                        await weaknessRepo.SaveChangesAsync(CancellationToken.None);
                    }

                    var mappedLevel = MapToTopicLevel(currentLevel);
                    var account = await accountRepo.GetByIdAsync(request.UserId);
                    if (account != null)
                    {
                        if (!account.Level.HasValue || (int)mappedLevel.Value > (int)account.Level.Value)
                        {
                            account.Level = mappedLevel;
                            account.UpdatedAt = DateTime.UtcNow;
                            await accountRepo.UpdateUserAsync(account);
                            await accountRepo.SaveChangesAsync(CancellationToken.None);
                            _logger.LogInformation("Cập nhật Level user {UserId}: → {NewLevel}",
                                request.UserId, mappedLevel);
                        }
                    }

                    _progressService.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId,
                        Percent = 100,
                        Step = "Lộ trình đã sẵn sàng!",
                        IsCompleted = true,
                        RoadmapId = roadmapId
                    });

                    _logger.LogInformation("Tạo lộ trình thành công — RoadmapId: {Id} | Job: {JobId}",
                        roadmapId, jobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi tạo Roadmap cho User: {UserId} | Job: {JobId}",
                        request.UserId, jobId);

                    _progressService.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId,
                        IsError = true,
                        Step = "Lỗi hệ thống.",
                        ErrorMessage = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại."
                    });
                }
            });
            return OperationResult<string>.Success(jobId, 202, "Đang tạo lộ trình, vui lòng theo dõi tiến trình.");
        }
        private static TopicLevel? MapToTopicLevel(CurrentTopikLevel level) => level switch
        {
            CurrentTopikLevel.Pre_Topik => TopicLevel.Level1,
            CurrentTopikLevel.Level_1 => TopicLevel.Level1,
            CurrentTopikLevel.Level_2 => TopicLevel.Level2,
            CurrentTopikLevel.Pre_Topik_II => TopicLevel.Level3,
            CurrentTopikLevel.Level_3 => TopicLevel.Level3,
            CurrentTopikLevel.Level_4 => TopicLevel.Level4,
            CurrentTopikLevel.Level_5 => TopicLevel.Level5,
            _ => TopicLevel.Level1
        };

        private static CurrentTopikLevel CalculateLevel(
            TargetAimLevel targetAim,
            double listeningScore,
            double readingScore,
            double writingScore)
        {
            if (targetAim == TargetAimLevel.Topik_I_Level1
             || targetAim == TargetAimLevel.Topik_I_Level2)
            {
                double topikIScore = listeningScore + readingScore;
                if (topikIScore >= 140) return CurrentTopikLevel.Level_2;
                if (topikIScore >= 80) return CurrentTopikLevel.Level_1;
                return CurrentTopikLevel.Pre_Topik;
            }

            double totalScore = listeningScore + readingScore + writingScore;
            if (totalScore >= 190) return CurrentTopikLevel.Level_5;
            if (totalScore >= 150) return CurrentTopikLevel.Level_4;
            if (totalScore >= 120) return CurrentTopikLevel.Level_3;
            return CurrentTopikLevel.Pre_Topik_II;
        }
        private void Report(string? jobId, int percent, string step)
        {
            if (string.IsNullOrEmpty(jobId)) return;
            _progressService.Set(jobId, new RoadmapProgressState
            {
                JobId = jobId,
                Percent = percent,
                Step = step
            });
        }
    }
}