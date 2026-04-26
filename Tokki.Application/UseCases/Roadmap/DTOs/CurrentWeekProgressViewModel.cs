using System;

namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class CurrentWeekProgressViewModel
    {
        public string RoadmapWeekId { get; set; } = string.Empty;
        public int WeekIndex { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int ProgressPercent { get; set; }
    }
}
