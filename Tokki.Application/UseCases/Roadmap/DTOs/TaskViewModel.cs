using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class TaskViewModel
    {
        public string TaskId { get; set; }
        public string Title { get; set; }
        public string TaskType { get; set; } 
        public bool IsCompleted { get; set; }
        public int DayIndex { get; set; }
        public string Content { get; set; }
        public string? ExamId { get; set; }
        public string? QuestionTypeId { get; set; } 
    }
}
