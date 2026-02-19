using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.MiniGame.DTOs
{
    public class WordleAiFeedbackDto
    {
        public bool ContainsTargetWord { get; set; } 
        public double TotalScore { get; set; } 

        public CriterionDto Meaning { get; set; } = new();

        public CriterionDto Grammar { get; set; } = new();

        public CriterionDto Naturalness { get; set; } = new();

        public string GeneralFeedback { get; set; } = string.Empty; 
        public string? CorrectedSentence { get; set; }
    }

    public class CriterionDto
    {
        public double Score { get; set; }
        public double MaxScore { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }
}
