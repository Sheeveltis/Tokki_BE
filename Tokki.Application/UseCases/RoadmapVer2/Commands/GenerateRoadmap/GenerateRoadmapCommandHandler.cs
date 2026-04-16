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

namespace Tokki.Application.UseCases.RoadmapVer2.Commands.GenerateRoadmap
{
    public class GenerateRoadmapCommandHandler : IRequestHandler<GenerateRoadmapCommand, OperationResult<string>>
    {
        private readonly IAiRoadmapVer2Service _aiRoadmapService;
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
            IAiRoadmapVer2Service aiRoadmapService,
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
            _logger.LogInformation("Bắt đầu tạo lộ trình tối ưu cho User: {UserId}", request.UserId);

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
                Step = "Đang khởi tạo luồng phân tích..."
            });

            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var sp = scope.ServiceProvider;

                var aiService = sp.GetRequiredService<IAiRoadmapVer2Service>();
                var assemblyService = sp.GetRequiredService<IExamAssemblyService>();
                var idGen = sp.GetRequiredService<IIdGeneratorService>();
                var roadmapRepo = sp.GetRequiredService<IUserRoadmapRepository>();
                var weaknessRepo = sp.GetRequiredService<IUserWeaknessRepository>();
                var userExamRepo = sp.GetRequiredService<IUserExamRepository>();
                var accountRepo = sp.GetRequiredService<IAccountRepository>();
                var med = sp.GetRequiredService<IMediator>();
                var progress = sp.GetRequiredService<IRoadmapProgressService>();

                try
                {
                    progress.Set(jobId, new RoadmapProgressState { JobId = jobId, Percent = 10, Step = "Bước 1: Phân tích bài thi đầu vào..." });

                    var currentLevel = CurrentTopikLevel.Pre_Topik;
                    var weaknesses = new List<string>();

                    if (!string.IsNullOrEmpty(request.UserExamId))
                    {
                        var initialExamResult = await med.Send(new GetUserExamResultQuery { UserExamId = request.UserExamId }, CancellationToken.None);

                        if (initialExamResult.IsSuccess && initialExamResult.Data != null)
                        {
                            var skillData = initialExamResult.Data;
                            currentLevel = CalculateLevel(request.TargetAim, skillData.Listening.Score, skillData.Reading.Score, skillData.Writing.Score);
                            
                            var inputUserExam = await userExamRepo.GetByIdAsync(request.UserExamId, CancellationToken.None);
                            if (inputUserExam != null && inputUserExam.SelfDeclaredLevel.HasValue)
                            {
                                // Nếu tự khai báo thấp hơn thực tế, lấy cái thấp hơn để an toàn
                                currentLevel = (CurrentTopikLevel)Math.Min((int)inputUserExam.SelfDeclaredLevel.Value, (int)currentLevel);
                            }

                            var types = await userExamRepo.GetIncorrectQuestionTypesByExamIdAsync(request.UserExamId, CancellationToken.None);
                            weaknesses = types.Select(t => t.QuestionTypeId).Distinct().ToList();
                        }
                    }

                    var newWeaknessEntities = new List<UserWeakness>();
                    if (weaknesses.Any())
                    {
                        progress.Set(jobId, new RoadmapProgressState { JobId = jobId, Percent = 30, Step = "Bước 2: Phân tích danh sách điểm yếu..." });
                        weaknesses = await roadmapRepo.GetValidQuestionTypeIdsAsync(weaknesses, CancellationToken.None);
                        
                        foreach (var wId in weaknesses)
                        {
                            newWeaknessEntities.Add(new UserWeakness
                            {
                                Id = idGen.GenerateCustom(15),
                                UserId = request.UserId,
                                QuestionTypeId = wId,
                                Status = 0, // Pending
                                Priority = 99, 
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    progress.Set(jobId, new RoadmapProgressState { JobId = jobId, Percent = 50, Step = "Bước 3: AI sắp xếp thứ tự ưu tiên học tập (FIFO)..." });

                    var weakTypeInfos = weaknesses.Any() 
                        ? await roadmapRepo.GetQuestionTypeMenuAsync(weaknesses, CancellationToken.None)
                        : new List<QuestionTypeMenuItem>();

                    if (weakTypeInfos.Any())
                    {
                        var orderedIds = await aiService.SequenceWeaknessesAsync(
                            weaknesses, 
                            currentLevel, 
                            request.TargetAim, 
                            weakTypeInfos.Select(x => new QuestionTypeMenuDto
                            {
                                QuestionTypeId = x.QuestionTypeId,
                                Code = x.Code,
                                Name = x.Name,
                                Skill = x.Skill
                            }).ToList(), 
                            CancellationToken.None);
                        
                        // Cập nhật Priority cho các entity trong bộ nhớ
                        for (int i = 0; i < orderedIds.Count; i++)
                        {
                            var entity = newWeaknessEntities.FirstOrDefault(e => e.QuestionTypeId == orderedIds[i]);
                            if (entity != null)
                            {
                                entity.Priority = i + 1;
                            }
                        }
                        
                        // Re-sort local records
                        weaknesses = orderedIds;
                        weakTypeInfos = weakTypeInfos.OrderBy(w => orderedIds.IndexOf(w.QuestionTypeId)).ToList();
                    }

                    // Bước 4: AI thiết kế tuần học đầu tiên
                    progress.Set(jobId, new RoadmapProgressState { JobId = jobId, Percent = 60, Step = "Bước 4: Thiết kế tuần học đầu tiên..." });
                    
                    var week1Types = weaknesses.Take(3).ToList();
                    var allLevelTypeIds = await roadmapRepo.GetValidQuestionTypeIdsByLevelAsync(currentLevel, CancellationToken.None);
                    var fullMenu = await roadmapRepo.GetQuestionTypeMenuAsync(allLevelTypeIds, CancellationToken.None);

                    int totalWeeks = (int)Math.Ceiling((double)request.DurationDays / 7);

                    var aiPlan = await aiService.GenerateStudyPlanAsync(
                        request.TargetAim,
                        currentLevel,
                        1,
                        totalWeeks,
                        week1Types,
                        new List<string>(), 
                        weakTypeInfos.Where(w => week1Types.Contains(w.QuestionTypeId)).Select(x => new QuestionTypeMenuDto
                        {
                            QuestionTypeId = x.QuestionTypeId,
                            Code = x.Code,
                            Name = x.Name,
                            Skill = x.Skill
                        }).ToList(),
                        fullMenu.Select(x => new QuestionTypeMenuDto
                        {
                            QuestionTypeId = x.QuestionTypeId,
                            Code = x.Code,
                            Name = x.Name,
                            Skill = x.Skill
                        }).ToList(),
                        CancellationToken.None);

                    if (aiPlan == null || !aiPlan.Weeks.Any())
                    {
                        progress.Set(jobId, new RoadmapProgressState { JobId = jobId, IsError = true, ErrorMessage = "AI không thể tạo nội dung học tập." });
                        return;
                    }

                    // Sanity Check: Kiểm tra dữ liệu rác
                    var firstWeek = aiPlan.Weeks.First();
                    if (string.IsNullOrEmpty(firstWeek.WeekGoal) || !firstWeek.Days.Any())
                    {
                        progress.Set(jobId, new RoadmapProgressState { JobId = jobId, IsError = true, ErrorMessage = "AI trả về dữ liệu thiếu thông tin tuần." });
                        return;
                    }

                    foreach (var day in firstWeek.Days)
                    {
                        if (day.DayIndex < 1 || !day.Tasks.Any() || day.Tasks.Any(t => string.IsNullOrEmpty(t.Title) || string.IsNullOrEmpty(t.Content)))
                        {
                            _logger.LogWarning("Dữ liệu rác từ AI cho User {UserId}: DayIndex={DayIndex}, TaskCount={Count}", request.UserId, day.DayIndex, day.Tasks.Count);
                            progress.Set(jobId, new RoadmapProgressState { JobId = jobId, IsError = true, ErrorMessage = "Dữ liệu AI không đạt tiêu chuẩn (Thiếu tiêu đề hoặc nội dung). Vui lòng thử lại." });
                            return;
                        }
                    }

                    progress.Set(jobId, new RoadmapProgressState { JobId = jobId, Percent = 80, Step = "Bước 5: Khởi tạo lộ trình và lưu trữ..." });

                    var roadmapId = idGen.GenerateCustom(15);
                    var roadmapEntity = new UserRoadmap
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
                        Weeks = new List<RoadmapWeek>()
                    };

                    // 1. Tạo WeeklyExam cho tuần 1
                    string? weeklyExamId = null;
                    var examType = (request.TargetAim == TargetAimLevel.Topik_I_Level1 || request.TargetAim == TargetAimLevel.Topik_I_Level2)
                        ? ExamType.TopikI : ExamType.TopikII;
                    var examResult = await assemblyService.GenerateWeeklyExamFromScopeAsync(request.UserId, 1, week1Types, examType, CancellationToken.None);
                    if (examResult.IsSuccess)
                    {
                        weeklyExamId = examResult.Data;
                    }

                    // 2. Khởi tạo tất cả các tuần
                    for (int i = 1; i <= totalWeeks; i++)
                    {
                        var weekId = idGen.GenerateCustom(15);
                        var weekEntity = new RoadmapWeek
                        {
                            RoadmapWeekId = weekId,
                            UserRoadmapId = roadmapId,
                            WeekIndex = i,
                            FromDate = roadmapEntity.StartDate.AddDays((i - 1) * 7),
                            ToDate = roadmapEntity.StartDate.AddDays(i * 7),
                            Status = (i == 1) ? RoadmapWeekStatus.InProgress : RoadmapWeekStatus.Locked,
                            DailyTasks = new List<RoadmapDailyTask>(),
                            WeeklyExamId = (i == 1) ? weeklyExamId : null
                        };

                        // Tuần 1: Fill dữ liệu từ AI
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

                                        // Fallback QuestionTypeId cho VirtualQuiz nếu AI quên
                                        string? finalQTypeId = taskDto.QuestionTypeId;
                                        if (taskEnum == RoadmapTaskType.VirtualQuiz && string.IsNullOrEmpty(finalQTypeId))
                                        {
                                            finalQTypeId = dayDto.Tasks.FirstOrDefault(t => t.TaskType == "LearnTheory")?.QuestionTypeId;
                                        }

                                        weekEntity.DailyTasks.Add(new RoadmapDailyTask
                                        {
                                            TaskId = idGen.GenerateCustom(15),
                                            RoadmapWeekId = weekId,
                                            DayIndex = dayDto.DayIndex,
                                            Title = taskDto.Title,
                                            TaskType = taskEnum,
                                            AiGeneratedContent = taskDto.Content,
                                            QuestionTypeId = finalQTypeId,
                                            ExamId = (taskEnum == RoadmapTaskType.WeeklyExam) ? weeklyExamId : null,
                                            IsCompleted = false
                                        });
                                    }
                                }
                            }
                        }
                        else
                        {
                            weekEntity.WeekFocusGoal = "Đang chờ kết quả tuần trước để tối ưu...";
                        }

                        roadmapEntity.Weeks.Add(weekEntity);
                    }

                    await roadmapRepo.AddAsync(roadmapEntity);
                    
                    // Gắn RoadmapId và lưu các điểm yếu mới
                    foreach (var nw in newWeaknessEntities)
                    {
                        nw.RoadmapId = roadmapId;
                        await weaknessRepo.AddAsync(nw, CancellationToken.None);
                    }

                    await roadmapRepo.SaveChangesAsync(CancellationToken.None);
                    await weaknessRepo.SaveChangesAsync(CancellationToken.None);

                    progress.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId,
                        Percent = 100,
                        Step = "Lộ trình cá nhân hóa (FIFO) đã sẵn sàng!",
                        IsCompleted = true,
                        RoadmapId = roadmapId
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi tạo Roadmap cho User {UserId}", request.UserId);
                    progress.Set(jobId, new RoadmapProgressState { JobId = jobId, IsError = true, ErrorMessage = "Lỗi hệ thống trong quá trình tạo lộ trình." });
                }
            });

            return OperationResult<string>.Success(jobId, 202, "Đang khởi tạo lộ trình ưu tiên điểm yếu theo trình tự AI (Ver 2)...");
        }

        private static CurrentTopikLevel CalculateLevel(TargetAimLevel targetAim, double L, double R, double W)
        {
            if (targetAim == TargetAimLevel.Topik_I_Level1 || targetAim == TargetAimLevel.Topik_I_Level2)
            {
                double score = L + R;
                if (score >= 140) return CurrentTopikLevel.Level_2;
                if (score >= 80) return CurrentTopikLevel.Level_1;
                return CurrentTopikLevel.Pre_Topik;
            }
            double total = L + R + W;
            if (total >= 230) return CurrentTopikLevel.Level_6;
            if (total >= 190) return CurrentTopikLevel.Level_5;
            if (total >= 150) return CurrentTopikLevel.Level_4;
            if (total >= 120) return CurrentTopikLevel.Level_3;
            return CurrentTopikLevel.Pre_Topik_II;
        }
    }
}
