using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.UserExam.DTOs
{
    public class UserExamResultResponse
    {
        public string UserExamId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public double TotalScore => Listening.Score + Reading.Score + Writing.Score;

        public bool IsGraded => Listening.IsGraded && Reading.IsGraded && Writing.IsGraded;
        public SkillScoreDto Listening { get; set; } = new();
        public SkillScoreDto Reading { get; set; } = new();
        public SkillScoreDto Writing { get; set; } = new();


    }

    public class SkillScoreDto
    {
        public double Score { get; set; }
        public double MaxScore { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsGraded { get; set; } = true; 
    }
}
