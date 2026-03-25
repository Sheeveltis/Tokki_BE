using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.DTOs
{
    public class ExamAnalysisResponse
    {
        public List<QuestionTypeDto> ReadingAnalysis { get; set; } = new();
        public List<QuestionTypeDto> ListeningAnalysis { get; set; } = new();
        public List<QuestionTypeDto> WritingAnalysis { get; set; } = new();
    }

    public class QuestionTypeDto
    {
        public string QuestionTypeId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public QuestionSkill Skill { get; set; }
        public bool IsWeakness { get; set; }
        public string WrongRatio { get; set; } = string.Empty; // e.g: "2/3"
    }
}
