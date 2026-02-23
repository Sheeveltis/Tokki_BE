using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Application.IServices
{
    public interface IAIPronunciationService
    {
        Task<(string Feedback, double FinalAccuracyScore)> GenerateFeedbackWithAudioAsync(
         PronunciationAssessmentDTO assessment,
         string targetText,
         string ruleContext,
         string base64Audio,
         string mimeType);
        Task<(string GeneralFeedback, double FinalAccuracyScore)> GenerateFeedbackAsync(
     PronunciationAssessmentDTO assessment,
     string targetText,
     string ruleContext);
    }
}
