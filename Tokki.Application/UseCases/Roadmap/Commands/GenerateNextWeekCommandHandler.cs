using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Application.UseCases.UserExam.Queries.GetExamAnalysis;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Commands.GenerateNextWeek
{
    public class GenerateNextWeekCommandHandler
        : IRequestHandler<GenerateNextWeekCommand, OperationResult<string>>
    {
        private readonly IUserRoadmapRepository _repository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IRoadmapProgressService _progressService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GenerateNextWeekCommandHandler> _logger;
        private const int MaxQuestionTypesPerWeek = 5;

        public GenerateNextWeekCommandHandler(
            IUserRoadmapRepository repository,
            IIdGeneratorService idGeneratorService,
            IRoadmapProgressService progressService,
            IServiceScopeFactory scopeFactory,
            ILogger<GenerateNextWeekCommandHandler> logger)
        {
            _repository           = repository;
            _idGeneratorService   = idGeneratorService;
            _progressService      = progressService;
            _scopeFactory         = scopeFactory;
            _logger               = logger;
        }

        public async Task<OperationResult<string>> Handle(
            GenerateNextWeekCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Chuyen tuan cho User: {UserId}, Tuan hien tai: {WeekId}",
                request.UserId, request.FinishedWeekId);

            var initialCheckWeek = await _repository
                .GetWeekByIdAsync(request.FinishedWeekId, cancellationToken);

            if (initialCheckWeek == null)
                return OperationResult<string>.Failure("Khong tim thay du lieu tuan hoc.", 404);

            if (initialCheckWeek.UserRoadmap.UserId != request.UserId)
                return OperationResult<string>.Failure("Khong co quyen truy cap.", 403);

            if (!string.IsNullOrEmpty(initialCheckWeek.WeeklyExamId))
            {
                var examSubmitted = await _repository.GetUserExamByExamIdAsync(
                    initialCheckWeek.WeeklyExamId, request.UserId, cancellationToken);

                if (examSubmitted == null)
                    return OperationResult<string>.Failure(
                        "Ban can hoan thanh bai kiem tra cuoi tuan truoc khi mo tuan moi.", 400);
            }

            var jobId = _idGeneratorService.GenerateCustom(15);
            _progressService.Set(jobId, new RoadmapProgressState
            {
                JobId   = jobId,
                Percent = 5,
                Step    = "Khoi tao tien trinh chuyen tuan va toi uu hoa lo trinh..."
            });

            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var sp = scope.ServiceProvider;

                var roadmapRepo  = sp.GetRequiredService<IUserRoadmapRepository>();
                var weaknessRepo = sp.GetRequiredService<IUserWeaknessRepository>();
                var aiService    = sp.GetRequiredService<IAiRoadmapService>();
                var assemblyService = sp.GetRequiredService<IExamAssemblyService>();
                var knowledgeRepo   = sp.GetRequiredService<IRoadmapKnowledgeProfileRepository>();
                var idGen        = sp.GetRequiredService<IIdGeneratorService>();
                var med          = sp.GetRequiredService<IMediator>();
                var progress     = sp.GetRequiredService<IRoadmapProgressService>();

                try
                {
                    var currentWeek = await roadmapRepo
                        .GetWeekByIdAsync(request.FinishedWeekId, CancellationToken.None);
                    if (currentWeek == null) return;

                    var roadmap = currentWeek.UserRoadmap;

                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId, Percent = 20,
                        Step = "Dang danh gia ket qua hoc tap tuan truoc de dieu chinh lo trinh..."
                    });

                    var deferredTypes = await EvaluateLastWeekPerformanceAsync(
                        currentWeek, knowledgeRepo, weaknessRepo, idGen, med, CancellationToken.None);

                    int nextWeekIndex = currentWeek.WeekIndex + 1;
                    var nextWeek = await roadmapRepo
                        .GetWeekByIndexAsync(roadmap.UserRoadmapId, nextWeekIndex, CancellationToken.None);
                    currentWeek.Status = RoadmapWeekStatus.Completed;

                    if (nextWeek == null)
                    {
                        await roadmapRepo.SaveChangesAsync(CancellationToken.None);
                        progress.Set(jobId, new RoadmapProgressState
                        {
                            JobId       = jobId,
                            Percent     = 100,
                            IsCompleted = true,
                            Step        = "Chuc mung! Ban da hoan thanh toan bo lo trinh hoc tap cua minh."
                        });
                        return;
                    }

                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId, Percent = 40,
                        Step = "Dang lua chon cac dang bai trong tam phu hop voi nang luc hien tai..."
                    });

                    var nextTypes = await SelectTypesForNextWeekAsync(
                        roadmap.UserRoadmapId, request.UserId, weaknessRepo, CancellationToken.None);

                    if (nextTypes.Count < MaxQuestionTypesPerWeek)
                    {
                        await FillWithDefaultTypesAsync(
                            nextTypes, roadmap.CurrentLevel, roadmapRepo, CancellationToken.None);
                    }

                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId, Percent = 60,
                        Step = "AI dang thiet ke noi dung bai hoc chi tiet cho tuan tiep theo..."
                    });

                    var weakTypeInfos    = await roadmapRepo.GetQuestionTypeMenuAsync(nextTypes, CancellationToken.None);
                    var allValidTypeIds  = await roadmapRepo.GetValidQuestionTypeIdsByLevelAsync(roadmap.CurrentLevel, CancellationToken.None);
                    var fullMenu         = await roadmapRepo.GetQuestionTypeMenuAsync(allValidTypeIds, CancellationToken.None);

                    int totalWeeks = (int)Math.Ceiling((roadmap.EndDate - roadmap.StartDate).TotalDays / 7);

                    int examScorePercent = deferredTypes.Any()
                        ? Math.Max(0, 70 - deferredTypes.Count * 15) 
                        : 75; 

                    var reviewTypes = nextTypes.Where(t => !deferredTypes.Contains(t)).ToList();

                    var allWeaknesses = (await weaknessRepo.GetByUserIdAsync(request.UserId, CancellationToken.None))
                        .Where(w => w.RoadmapId == roadmap.UserRoadmapId)
                        .Select(w => w.QuestionTypeId).ToList();

                    var aiPlan = await aiService.GenerateNextWeekPlanAsync(
                        roadmap.TargetAim,
                        roadmap.CurrentLevel,
                        nextWeekIndex,
                        examScorePercent,
                        reviewTypes,
                        deferredTypes,
                        allWeaknesses,
                        weakTypeInfos,
                        fullMenu);

                    if (aiPlan == null || !aiPlan.Weeks.Any())
                    {
                        progress.Set(jobId, new RoadmapProgressState
                        {
                            JobId = jobId, IsError = true,
                            ErrorMessage = "AI khong the thiet ke noi dung cho tuan moi luc nay."
                        });
                        return;
                    }

                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId, Percent = 85,
                        Step = "Dang chuan bi bai thi tong hop va luu tru du lieu tuan moi..."
                    });

                    await SaveNextWeekTasksAsync(
                        nextWeek, aiPlan.Weeks.First(), nextTypes,
                        roadmap.TargetAim, request.UserId,
                        roadmapRepo, assemblyService, idGen, allValidTypeIds, CancellationToken.None);

                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId       = jobId,
                        Percent     = 100,
                        IsCompleted = true,
                        Step        = "Tien trinh toi uu lo trinh hoan tat. Tuan hoc moi da san sang!"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Loi khi chuyen tuan cho User {UserId}", request.UserId);
                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId        = jobId,
                        IsError      = true,
                        ErrorMessage = "Da xay ra loi trong qua trinh xu ly tuan hoc moi."
                    });
                }
            });

            return OperationResult<string>.Success(jobId, 202,
                "Dang bat dau phan tich du lieu va thiet ke tuan hoc moi...");
        }

        private async Task<List<string>> EvaluateLastWeekPerformanceAsync(
            RoadmapWeek currentWeek,
            IRoadmapKnowledgeProfileRepository knowledgeRepo,
            IUserWeaknessRepository weaknessRepo,
            IIdGeneratorService idGen,
            IMediator med,
            CancellationToken token)
        {
            var deferredTypes = new List<string>();
            if (string.IsNullOrEmpty(currentWeek.WeeklyExamId)) return deferredTypes;

            var analysis = await med.Send(new GetExamAnalysisQuery(currentWeek.WeeklyExamId), token);
            if (!analysis.IsSuccess || analysis.Data == null) return deferredTypes;

            var allAnalysis = analysis.Data.ReadingAnalysis
                .Concat(analysis.Data.ListeningAnalysis)
                .Concat(analysis.Data.WritingAnalysis)
                .ToList();

            var weekTypeIds = currentWeek.DailyTasks
                .Where(t => t.QuestionTypeId != null)
                .Select(t => t.QuestionTypeId!)
                .Distinct().ToList();

            var currentRoadmapWeaknesses = (await weaknessRepo
                .GetByUserIdAsync(currentWeek.UserRoadmap.UserId, token))
                .Where(w => w.RoadmapId == currentWeek.UserRoadmapId)
                .ToList();

            int maxPriority = currentRoadmapWeaknesses.Any()
                ? currentRoadmapWeaknesses.Max(w => w.Priority) : 0;

            foreach (var typeId in weekTypeIds)
            {
                var stat     = allAnalysis.FirstOrDefault(a => a.QuestionTypeId == typeId);
                bool isPassed = stat != null && !stat.IsWeakness;

                var profile = await knowledgeRepo.GetAsync(currentWeek.UserRoadmapId, typeId, token);
                if (profile == null)
                {
                    profile = new RoadmapKnowledgeProfile
                    {
                        ProfileId              = idGen.GenerateCustom(15),
                        UserRoadmapId          = currentWeek.UserRoadmapId,
                        QuestionTypeId         = typeId,
                        IsWeakness             = !isPassed,
                        ConsecutiveFailWeeks   = isPassed ? 0 : 1,
                        LastEvaluatedWeekIndex = currentWeek.WeekIndex
                    };
                    await knowledgeRepo.AddAsync(profile, token);
                }
                else
                {
                    profile.IsWeakness             = !isPassed;
                    profile.ConsecutiveFailWeeks   = isPassed ? 0 : profile.ConsecutiveFailWeeks + 1;
                    profile.LastEvaluatedWeekIndex = currentWeek.WeekIndex;
                }

                var userWeakness = currentRoadmapWeaknesses.FirstOrDefault(w => w.QuestionTypeId == typeId);
                if (userWeakness == null)
                {
                    userWeakness = new UserWeakness
                    {
                        Id             = idGen.GenerateCustom(15),
                        UserId         = currentWeek.UserRoadmap.UserId,
                        RoadmapId      = currentWeek.UserRoadmapId,
                        QuestionTypeId = typeId,
                        Status         = 0,
                        Priority       = ++maxPriority,
                        CreatedAt      = DateTime.UtcNow
                    };
                    await weaknessRepo.AddAsync(userWeakness, token);
                    currentRoadmapWeaknesses.Add(userWeakness);
                }

                if (isPassed) userWeakness.Status = 2; 
                else          userWeakness.Status = 1; 

                if (profile.ConsecutiveFailWeeks >= 2)
                {
                    userWeakness.Priority = ++maxPriority;
                    deferredTypes.Add(typeId);
                }
            }

            await knowledgeRepo.SaveChangesAsync(token);
            await weaknessRepo.SaveChangesAsync(token);
            return deferredTypes;
        }

        private async Task<List<string>> SelectTypesForNextWeekAsync(
            string roadmapId, string userId,
            IUserWeaknessRepository weaknessRepo, CancellationToken token)
        {
            var weaknesses = await weaknessRepo.GetByUserIdAsync(userId, token);
            var currentRoadmapWeaknesses = weaknesses
                .Where(w => w.RoadmapId == roadmapId)
                .ToList();

            return currentRoadmapWeaknesses
                .Where(w => w.Status == 0 || w.Status == 1)
                .OrderBy(w => w.Priority)
                .Select(w => w.QuestionTypeId)
                .Take(MaxQuestionTypesPerWeek)
                .ToList();
        }

        private async Task FillWithDefaultTypesAsync(
            List<string> types, CurrentTopikLevel level,
            IUserRoadmapRepository roadmapRepo, CancellationToken token)
        {
            var fallbackIds = await roadmapRepo.GetValidQuestionTypeIdsByLevelAsync(level, token);
            var extras = fallbackIds.Except(types).Take(MaxQuestionTypesPerWeek - types.Count);
            types.AddRange(extras);
        }

        private async Task SaveNextWeekTasksAsync(
            RoadmapWeek nextWeek,
            AiWeekPlan weekData,
            List<string> types,
            TargetAimLevel target,
            string userId,
            IUserRoadmapRepository roadmapRepo,
            IExamAssemblyService assemblyService,
            IIdGeneratorService idGen,
            List<string> allValidTypeIds,
            CancellationToken token)
        {
            nextWeek.WeekFocusGoal = weekData.WeekGoal;
            nextWeek.Status        = RoadmapWeekStatus.InProgress;
            nextWeek.DailyTasks.Clear();

            foreach (var dayDto in weekData.Days)
            {
                foreach (var taskDto in dayDto.Tasks)
                {
                    string? finalQTypeId = taskDto.QuestionTypeId;
                    if (string.IsNullOrWhiteSpace(finalQTypeId) ||
                        finalQTypeId.Equals("null", StringComparison.OrdinalIgnoreCase))
                        finalQTypeId = null;

                    if (finalQTypeId != null && !allValidTypeIds.Contains(finalQTypeId))
                    {
                        _logger.LogWarning(
                            "AI returned invalid QuestionTypeId: {InvalidId} for User {UserId}. Setting to null.",
                            finalQTypeId, userId);
                        finalQTypeId = null;
                    }

                    var taskEnum = RoadmapTaskType.LearnTheory;
                    Enum.TryParse(taskDto.TaskType, true, out taskEnum);

                    var taskEntity = new RoadmapDailyTask
                    {
                        TaskId             = idGen.GenerateCustom(15),
                        RoadmapWeekId      = nextWeek.RoadmapWeekId,
                        DayIndex           = dayDto.DayIndex,
                        Title              = taskDto.Title,
                        AiGeneratedContent = taskDto.Content,
                        IsCompleted        = false,
                        QuestionTypeId     = finalQTypeId,
                        TaskType           = taskEnum
                    };

                    if (taskEnum == RoadmapTaskType.WeeklyExam)
                    {
                        var examType = (target == TargetAimLevel.Topik_I_Level1
                            || target == TargetAimLevel.Topik_I_Level2)
                            ? ExamType.TopikI : ExamType.TopikII;

                        var examResult = await assemblyService.GenerateWeeklyExamFromScopeAsync(
                            userId, nextWeek.WeekIndex, types, examType, token);

                        if (examResult.IsSuccess)
                        {
                            taskEntity.ExamId      = examResult.Data;
                            nextWeek.WeeklyExamId  = examResult.Data;
                        }
                    }

                    nextWeek.DailyTasks.Add(taskEntity);
                }
            }

            await roadmapRepo.SaveChangesAsync(token);
        }
    }
}