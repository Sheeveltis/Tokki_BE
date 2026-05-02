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
                var idGen             = sp.GetRequiredService<IIdGeneratorService>();
                var roadmapRepo = sp.GetRequiredService<IUserRoadmapRepository>();
                var weaknessRepo = sp.GetRequiredService<IUserWeaknessRepository>();
                var userExamRepo = sp.GetRequiredService<IUserExamRepository>();
                var accountRepo = sp.GetRequiredService<IAccountRepository>();
                var mediator          = sp.GetRequiredService<IMediator>();
                var progress          = sp.GetRequiredService<IRoadmapProgressService>();

                try
                {
                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId, Percent = 10,
                        Step = "Buoc 1: Phan tich bai thi dau vao..."
                    });

                    var currentLevel = CurrentTopikLevel.Pre_Topik;
                    var weaknesses   = new List<string>();

                    if (!string.IsNullOrEmpty(request.UserExamId))
                    {
                        var examResult = await mediator.Send(
                            new GetUserExamResultQuery { UserExamId = request.UserExamId },
                            CancellationToken.None);

                        if (examResult.IsSuccess && examResult.Data != null)
                        {
                            var skillData = examResult.Data;
                            currentLevel = CalculateLevel(
                                request.TargetAim,
                                skillData.Listening.Score,
                                skillData.Reading.Score,
                                skillData.Writing.Score);

                            var selfDeclaredLevel = await userExamRepo
                                .GetSelfDeclaredLevelAsync(request.UserExamId, CancellationToken.None);
                            if (selfDeclaredLevel != null)
                                currentLevel = (CurrentTopikLevel)Math.Min(
                                    (int)selfDeclaredLevel.Value, (int)currentLevel);

                            var types = await userExamRepo
                                .GetIncorrectQuestionTypesByExamIdAsync(request.UserExamId, CancellationToken.None);
                            weaknesses = types.Select(t => t.QuestionTypeId).Distinct().ToList();
                        }
                    }

                    var newWeaknessEntities = new List<UserWeakness>();
                    if (weaknesses.Any())
                    {
                        progress.Set(jobId, new RoadmapProgressState
                        {
                            JobId = jobId, Percent = 25,
                            Step = "Buoc 2: Phan tich danh sach diem yeu..."
                        });

                        var validIds = await roadmapRepo
                            .GetValidQuestionTypeIdsAsync(weaknesses, CancellationToken.None);

                        var invalidIds = weaknesses.Except(validIds).ToList();
                        if (invalidIds.Any())
                            _logger.LogWarning("Loại bỏ {Count} questionTypeId không hợp lệ: {Ids}",
                                invalidIds.Count, string.Join(", ", invalidIds));

                        weaknesses = validIds;

                        foreach (var wId in weaknesses)
                        {
                            newWeaknessEntities.Add(new UserWeakness
                            {
                                Id             = idGen.GenerateCustom(15),
                                UserId         = request.UserId,
                                QuestionTypeId = wId,
                                Status         = 0,
                                Priority       = 99, 
                                CreatedAt      = DateTime.UtcNow
                            });
                        }
                    }

                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId, Percent = 40,
                        Step = "Buoc 3: AI sap xep thu tu uu tien hoc tap (FIFO)..."
                    });

                    var weakTypeInfos = weaknesses.Any()
                        ? await roadmapRepo.GetQuestionTypeMenuAsync(weaknesses, CancellationToken.None)
                        : new List<QuestionTypeMenuItem>();

                    if (weakTypeInfos.Any())
                    {
                        var orderedIds = await aiRoadmapService.SequenceWeaknessesAsync(
                            weaknesses, currentLevel, request.TargetAim, weakTypeInfos, CancellationToken.None);

                        for (int i = 0; i < orderedIds.Count; i++)
                        {
                            var entity = newWeaknessEntities.FirstOrDefault(e => e.QuestionTypeId == orderedIds[i]);
                            if (entity != null) entity.Priority = i + 1;
                        }

                        weaknesses    = orderedIds;
                        weakTypeInfos = weakTypeInfos
                            .OrderBy(w => orderedIds.IndexOf(w.QuestionTypeId))
                            .ToList();
                    }

                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId, Percent = 55,
                        Step = "Buoc 4: Thiet ke tuan hoc dau tien..."
                    });

                    var week1Types = weaknesses.Take(3).ToList();

                    var allLevelTypeIds = await roadmapRepo
                        .GetValidQuestionTypeIdsByLevelAsync(currentLevel, CancellationToken.None);

                    var fullMenu = await roadmapRepo
                        .GetQuestionTypeMenuAsync(allLevelTypeIds, CancellationToken.None);

                    int totalWeeks = (int)Math.Ceiling((double)request.DurationDays / 7);

                    var aiPlan = await aiRoadmapService.GenerateStudyPlanAsync(
                        request.TargetAim,
                        currentLevel,
                        request.DurationDays,
                        week1Types,
                        weakTypeInfos.Where(w => week1Types.Contains(w.QuestionTypeId)).ToList(),
                        fullMenu,
                        Math.Min(week1Types.Count, 3),
                        totalWeeks);

                    if (aiPlan == null || !aiPlan.Weeks.Any())
                    {
                        progress.Set(jobId, new RoadmapProgressState
                        {
                            JobId = jobId, IsError = true,
                            Step = "AI khong the tao lo trinh. Vui long thu lai.",
                            ErrorMessage = "AI khong the tao lo trinh. Vui long thu lai."
                        });
                        return;
                    }

                    var firstWeek = aiPlan.Weeks.First();
                    if (string.IsNullOrEmpty(firstWeek.WeekGoal) || !firstWeek.Days.Any())
                    {
                        progress.Set(jobId, new RoadmapProgressState
                        {
                            JobId = jobId, IsError = true,
                            ErrorMessage = "AI tra ve du lieu thieu thong tin tuan."
                        });
                        return;
                    }

                    foreach (var day in firstWeek.Days)
                    {
                        if (day.DayIndex < 1 || !day.Tasks.Any() ||
                            day.Tasks.Any(t => string.IsNullOrEmpty(t.Title) || string.IsNullOrEmpty(t.Content)))
                        {
                            _logger.LogWarning("Du lieu rac tu AI cho User {UserId}: DayIndex={DayIndex}",
                                request.UserId, day.DayIndex);
                            progress.Set(jobId, new RoadmapProgressState
                            {
                                JobId = jobId, IsError = true,
                                ErrorMessage = "Du lieu AI khong dat tieu chuan (Thieu tieu de hoac noi dung). Vui long thu lai."
                            });
                            return;
                        }
                    }

                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId, Percent = 70,
                        Step = "Buoc 5: Khoi tao lo trinh va luu tru..."
                    });

                    var roadmapId = idGen.GenerateCustom(15);
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

                    string? weeklyExamId = null;
                    var examType = (request.TargetAim == TargetAimLevel.Topik_I_Level1
                        || request.TargetAim == TargetAimLevel.Topik_I_Level2)
                        ? ExamType.TopikI : ExamType.TopikII;

                    if (week1Types.Any())
                    {
                        var examResult = await examAssemblyService.GenerateWeeklyExamFromScopeAsync(
                            request.UserId, 1, week1Types, examType, CancellationToken.None);
                        if (examResult.IsSuccess)
                            weeklyExamId = examResult.Data;
                    }

                    for (int i = 1; i <= totalWeeks; i++)
                    {
                        var weekId = idGen.GenerateCustom(15);
                        var weekEntity = new RoadmapWeek
                        {
                            RoadmapWeekId = weekId,
                            UserRoadmapId = roadmapId,
                            WeekIndex = i,
                            FromDate = roadmap.StartDate.AddDays((i - 1) * 7),
                            ToDate = roadmap.StartDate.AddDays(i * 7),
                            Status        = (i == 1) ? RoadmapWeekStatus.InProgress : RoadmapWeekStatus.Locked,
                            WeeklyExamId  = (i == 1) ? weeklyExamId : null,
                            DailyTasks    = new List<RoadmapDailyTask>()
                        };

                        if (i == 1)
                        {
                            var weekDto = aiPlan.Weeks.FirstOrDefault();
                            if (weekDto != null)
                            {
                                weekEntity.WeekFocusGoal = weekDto.WeekGoal;

                                foreach (var dayDto in weekDto.Days)
                                {
                                    foreach (var taskDto in dayDto.Tasks)
                                    {
                                        var taskEnum = RoadmapTaskType.LearnTheory;
                                        Enum.TryParse(taskDto.TaskType, true, out taskEnum);

                                        string? finalQTypeId = taskDto.QuestionTypeId;
                                        if (string.IsNullOrWhiteSpace(finalQTypeId) ||
                                            finalQTypeId.Equals("null", StringComparison.OrdinalIgnoreCase))
                                            finalQTypeId = null;

                                        if (taskEnum == RoadmapTaskType.VirtualQuiz && string.IsNullOrEmpty(finalQTypeId))
                                            finalQTypeId = dayDto.Tasks
                                                .FirstOrDefault(t => t.TaskType == "LearnTheory")?.QuestionTypeId;

                                        if (finalQTypeId != null && !allLevelTypeIds.Contains(finalQTypeId))
                                        {
                                            _logger.LogWarning(
                                                "AI returned invalid QuestionTypeId: {InvalidId} for User {UserId}. Setting to null.",
                                                finalQTypeId, request.UserId);
                                            finalQTypeId = null;
                                        }

                                        weekEntity.DailyTasks.Add(new RoadmapDailyTask
                                        {
                                            TaskId             = idGen.GenerateCustom(15),
                                            RoadmapWeekId      = weekId,
                                            DayIndex           = dayDto.DayIndex,
                                            Title              = taskDto.Title,
                                            TaskType           = taskEnum,
                                            AiGeneratedContent = taskDto.Content,
                                            QuestionTypeId     = finalQTypeId,
                                            ExamId             = (taskEnum == RoadmapTaskType.WeeklyExam) ? weeklyExamId : null,
                                            IsCompleted        = false
                                        });
                                    }
                                }
                            }
                        }
                        else
                        {
                            weekEntity.WeekFocusGoal = "Dang cho ket qua tuan truoc de toi uu...";
                        }

                        roadmap.Weeks.Add(weekEntity);
                    }

                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId, Percent = 85,
                        Step = "Buoc 6: Luu lo trinh va cap nhat ho so hoc vien..."
                    });

                    await roadmapRepo.AddAsync(roadmap);

                    foreach (var nw in newWeaknessEntities)
                    {
                        nw.RoadmapId = roadmapId;
                        await weaknessRepo.AddAsync(nw, CancellationToken.None);
                    }

                    await roadmapRepo.SaveChangesAsync(CancellationToken.None);
                    await weaknessRepo.SaveChangesAsync(CancellationToken.None);

                    var mappedLevel = MapToTopicLevel(currentLevel);
                    var account = await accountRepo.GetByIdAsync(request.UserId);
                    if (account != null && mappedLevel.HasValue)
                    {
                        int newLevelVal = (int)mappedLevel.Value;
                        if (!account.Level.HasValue || newLevelVal > account.Level.Value)
                        {
                            account.Level = newLevelVal;
                            account.UpdatedAt = DateTime.UtcNow;
                            await accountRepo.UpdateUserAsync(account);
                            await accountRepo.SaveChangesAsync(CancellationToken.None);
                            _logger.LogInformation("Cập nhật Level user {UserId}: → {NewLevel}",
                                request.UserId, mappedLevel);
                        }
                    }

                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId,
                        Percent      = 100,
                        Step         = "Lo trinh ca nhan hoa da san sang!",
                        IsCompleted  = true,
                        RoadmapId    = roadmapId
                    });

                    _logger.LogInformation("Tạo lộ trình thành công — RoadmapId: {Id} | Job: {JobId}",
                        roadmapId, jobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi tạo Roadmap cho User: {UserId} | Job: {JobId}",
                        request.UserId, jobId);

                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId         = jobId,
                        IsError       = true,
                        Step          = "Loi he thong.",
                        ErrorMessage  = "Da xay ra loi khong mong muon. Vui long thu lai."
                    });
                }
            });

            return OperationResult<string>.Success(jobId, 202,
                "Dang tao lo trinh, vui long theo doi tien trinh.");
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
                if (topikIScore >= 80)  return CurrentTopikLevel.Level_1;
                return CurrentTopikLevel.Pre_Topik;
            }

            double total = listeningScore + readingScore + writingScore;
            if (total >= 230) return CurrentTopikLevel.Level_6; // Them Level_6 tu Ver2
            if (total >= 190) return CurrentTopikLevel.Level_5;
            if (total >= 150) return CurrentTopikLevel.Level_4;
            if (total >= 120) return CurrentTopikLevel.Level_3;
            return CurrentTopikLevel.Pre_Topik_II;
        }
    }
}