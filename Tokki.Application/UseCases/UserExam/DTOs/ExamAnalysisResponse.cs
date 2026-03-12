using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.UserExam.DTOs
{
    public class ExamAnalysisResponse
    {
        public List<QuestionTypeDto> ReadingIssues { get; set; } = new();
        public List<QuestionTypeDto> ListeningIssues { get; set; } = new();
        public List<QuestionTypeDto> WritingIssues { get; set; } = new();
    }

    public class QuestionTypeDto
    {
        public string QuestionTypeId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
