using System.Threading.Tasks;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Application.IServices
{
    public interface ISpeechService
    {
        Task<byte[]> SynthesizeKoreanAudioAsync(string text);
        Task<PronunciationAssessmentDTO> AssessPronunciationAsync(Stream audioStream, string referenceText);
    }
}