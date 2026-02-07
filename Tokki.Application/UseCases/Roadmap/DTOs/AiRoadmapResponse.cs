using System.Collections.Generic;

namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class AiRoadmapResponse
    {
        public string Assessment { get; set; } = string.Empty;
        public List<AiWeekPlan> Weeks { get; set; } = new List<AiWeekPlan>();
    }

    public class AiWeekPlan
    {
        public int WeekIndex { get; set; }
        public string WeekGoal { get; set; } = string.Empty;
        public List<AiDaySchedule> Days { get; set; } = new List<AiDaySchedule>();
    }

    public class AiDaySchedule
    {
        public int DayIndex { get; set; }
        public List<AiTask> Tasks { get; set; } = new List<AiTask>();
    }

    public class AiTask
    {
        public string Title { get; set; } = string.Empty;

        public string TaskType { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }
}