using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class RoadmapViewModel
    {
        public string UserRoadmapId { get; set; }
        public string TargetAim { get; set; }
        public string Assessment { get; set; }
        public int ProgressPercent { get; set; } 
        public List<WeekViewModel> Weeks { get; set; }
    }
}
