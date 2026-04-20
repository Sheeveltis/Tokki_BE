using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.RoadmapVer2.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.RoadmapVer2.Queries.GetCurrentRoadmap
{
    public class GetCurrentRoadmapVer2QueryHandler : IRequestHandler<GetCurrentRoadmapVer2Query, OperationResult<CurrentRoadmapVer2ViewModel>>
    {
        private readonly IUserRoadmapRepository _userRoadmapRepository;

        public GetCurrentRoadmapVer2QueryHandler(IUserRoadmapRepository userRoadmapRepository)
        {
            _userRoadmapRepository = userRoadmapRepository;
        }

        public async Task<OperationResult<CurrentRoadmapVer2ViewModel>> Handle(GetCurrentRoadmapVer2Query request, CancellationToken cancellationToken)
        {
            var roadmap = await _userRoadmapRepository.GetActiveRoadmapByUserIdAsync(request.UserId, cancellationToken);
            if (roadmap == null)
            {
                return OperationResult<CurrentRoadmapVer2ViewModel>.Failure("Người dùng chưa có lộ trình học nào đang kích hoạt.", 404);
            }

            // Tìm tuần hiện tại
            var currentWeek = roadmap.Weeks
                .OrderBy(w => w.WeekIndex)
                .FirstOrDefault(w => w.Status == RoadmapWeekStatus.InProgress);

            if (currentWeek == null)
            {
                currentWeek = roadmap.Weeks
                    .OrderBy(w => w.WeekIndex)
                    .LastOrDefault(w => w.Status != RoadmapWeekStatus.Locked);
            }

            if (currentWeek == null)
            {
                currentWeek = roadmap.Weeks.OrderBy(w => w.WeekIndex).FirstOrDefault();
            }

            if (currentWeek == null)
            {
                return OperationResult<CurrentRoadmapVer2ViewModel>.Failure("Không tìm thấy dữ liệu lộ trình.", 404);
            }

            // Map ViewModel
            var viewModel = new CurrentRoadmapVer2ViewModel
            {
                UserRoadmapId = roadmap.UserRoadmapId,
                TargetAim = roadmap.TargetAim,
                CurrentLevel = roadmap.CurrentLevel,
                DurationDays = roadmap.DurationDays,
                StartDate = roadmap.StartDate,
                OverallAiAssessment = roadmap.OverallAiAssessment ?? "",
                TotalProgressPercent = roadmap.Weeks.SelectMany(w => w.DailyTasks).Any()
                    ? (int)((double)roadmap.Weeks.SelectMany(w => w.DailyTasks).Count(t => t.IsCompleted) / roadmap.Weeks.SelectMany(w => w.DailyTasks).Count() * 100)
                    : 0,
                CurrentWeek = new CurrentWeekVer2Dto
                {
                    RoadmapWeekId = currentWeek.RoadmapWeekId,
                    WeekIndex = currentWeek.WeekIndex,
                    WeekFocusGoal = currentWeek.WeekFocusGoal ?? "",
                    Status = currentWeek.Status,
                    WeeklyExamId = currentWeek.WeeklyExamId,
                    WeekProgressPercent = currentWeek.DailyTasks.Any()
                        ? (int)((double)currentWeek.DailyTasks.Count(t => t.IsCompleted) / currentWeek.DailyTasks.Count * 100)
                        : 0,
                    Days = currentWeek.DailyTasks
                        .GroupBy(t => t.DayIndex)
                        .OrderBy(g => g.Key)
                        .Select(g => new DailyTasksGroupDto
                        {
                            DayIndex = g.Key,
                            Tasks = g.OrderBy(t => t.TaskId).Select(t => new RoadmapTaskSummaryDto
                            {
                                TaskId = t.TaskId,
                                Title = t.Title,
                                TaskType = t.TaskType,
                                QuestionTypeId = t.QuestionTypeId ?? t.TargetQuestionTypeId,
                                ExamId = t.ExamId,
                                IsCompleted = t.IsCompleted,
                                HasContent = !string.IsNullOrEmpty(t.AiGeneratedContent)
                            }).ToList()
                        }).ToList()
                }
            };

            return OperationResult<CurrentRoadmapVer2ViewModel>.Success(viewModel);
        }
    }
}
