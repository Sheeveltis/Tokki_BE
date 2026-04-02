using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.PronunciationRule.DTOs
{
    public class PronunciationResponse : PronunciationAssessmentDTO
    {
        public string AiFeedback { get; set; } = string.Empty;
        public bool IsIrrelevant { get; set; } = false;
    }
}
