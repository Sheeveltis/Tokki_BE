using System.Threading.Tasks;

namespace Tokki.Application.IServices
{
    public interface ITextToSpeechService
    {
        Task<byte[]> SynthesizeKoreanAudioAsync(string text);
    }
}