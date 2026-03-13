using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.UserExam.DTOs
{
    public class SubmitExamResponse
    {
        public string UserExamId { get; set; } = string.Empty;
        public int FinalMcqScore { get; set; }
        public int TimeSpentMinutes { get; set; }
    }
}
