using System;
using System.Collections.Generic;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.RoadmapVer2.DTOs
{
    public class CurrentRoadmapVer2ViewModel
    {
        public string UserRoadmapId { get; set; } = string.Empty;
        public TargetAimLevel TargetAim { get; set; }
        public CurrentTopikLevel CurrentLevel { get; set; }
        public int DurationDays { get; set; }
        public DateTime StartDate { get; set; }
        public int TotalProgressPercent { get; set; }
        public string OverallAiAssessment { get; set; } = string.Empty;

        public CurrentWeekVer2Dto CurrentWeek { get; set; } = new();
    }

    public class CurrentWeekVer2Dto
    {
        public string RoadmapWeekId { get; set; } = string.Empty;
        public int WeekIndex { get; set; }
        public string WeekFocusGoal { get; set; } = string.Empty;
        public RoadmapWeekStatus Status { get; set; }
        public List<DailyTasksGroupDto> Days { get; set; } = new();
        public int WeekProgressPercent { get; set; }
        public string? WeeklyExamId { get; set; }
    }

    public class DailyTasksGroupDto
    {
        public int DayIndex { get; set; }
        public List<RoadmapTaskSummaryDto> Tasks { get; set; } = new();
    }

    public class RoadmapTaskSummaryDto
    {
        public string TaskId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public RoadmapTaskType TaskType { get; set; }
        public string? QuestionTypeId { get; set; }
        public string? ExamId { get; set; }
        public bool IsCompleted { get; set; }
        public bool HasContent { get; set; } // Để UI biết có cần gọi API lấy Content không
    }
}
