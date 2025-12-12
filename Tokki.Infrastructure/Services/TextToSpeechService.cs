using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Tokki.Application.IServices;

namespace Tokki.Infrastructure.Services
{
    public class TextToSpeechService : ITextToSpeechService
    {
        private readonly string _speechKey;
        private readonly string _speechRegion;

        public TextToSpeechService(IConfiguration configuration)
        {
            _speechKey = configuration["AzureSpeech:Key"];
            _speechRegion = configuration["AzureSpeech:Region"];
        }

        public async Task<byte[]> SynthesizeKoreanAudioAsync(string text)
        {
            var config = SpeechConfig.FromSubscription(_speechKey, _speechRegion);

            config.SpeechSynthesisVoiceName = "ko-KR-SunHiNeural";

            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3);

            using var synthesizer = new SpeechSynthesizer(config, null);

            var result = await synthesizer.SpeakTextAsync(text);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                return result.AudioData;
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                throw new Exception($"Azure TTS Error: {cancellation.Reason} - {cancellation.ErrorDetails}");
            }

            throw new Exception("Lỗi không xác định khi tạo Audio.");
        }
    }
}