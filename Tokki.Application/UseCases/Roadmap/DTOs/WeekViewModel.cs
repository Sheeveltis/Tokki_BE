using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class WeekViewModel
    {
        public string RoadmapWeekId { get; set; }
        public int WeekIndex { get; set; }
        public string FocusGoal { get; set; }
        public string Status { get; set; } 
        public List<TaskViewModel> Tasks { get; set; }
    }
}
