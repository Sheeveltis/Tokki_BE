using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.PronunciationRule.DTOs
{
    public class PronunciationAssessmentDTO
    {
        public double AccuracyScore { get; set; }    
        public double FluencyScore { get; set; }    
        public double CompletenessScore { get; set; } 
        public double ProsodyScore { get; set; }     

        public List<WordAssessmentDTO> Words { get; set; } = new List<WordAssessmentDTO>();
    }

    public class WordAssessmentDTO
    {
        public string Word { get; set; } = string.Empty;
        public double AccuracyScore { get; set; }
        public string ErrorType { get; set; } = string.Empty; 
        public string? Phonemes { get; set; }     
    }
}
