using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Infrastructure.Services
{
    public class SpeechService : ISpeechService
    {
        private readonly string _speechKey;
        private readonly string _speechRegion;

        public SpeechService(IConfiguration configuration)
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

            if (result.Reason == ResultReason.SynthesizingAudioCompleted) return result.AudioData;
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                throw new Exception($"Azure TTS Error: {cancellation.Reason}");
            }
            throw new Exception("Lỗi tạo Audio.");
        }

        // --- Phần STT & Chấm điểm (SỬA LẠI ĐOẠN NÀY) ---
        public async Task<PronunciationAssessmentDTO> AssessPronunciationAsync(Stream audioStream, string referenceText)
        {
            byte[] pcmData = ConvertAudioToPcm16Khz(audioStream);

            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            speechConfig.SpeechRecognitionLanguage = "ko-KR";

            var pronunciationConfig = new PronunciationAssessmentConfig(
                referenceText,
                GradingSystem.HundredMark,
                Granularity.Phoneme,
                enableMiscue: true);
            pronunciationConfig.PhonemeAlphabet = "IPA";
            var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1);

            using var convertedStream = new MemoryStream(pcmData);
            using var reader = new BinaryAudioStreamReader(convertedStream);
            using var audioInputStream = AudioInputStream.CreatePullStream(reader, audioFormat);
            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);

            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
            pronunciationConfig.ApplyTo(recognizer);

            var result = await recognizer.RecognizeOnceAsync();

            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                var assessmentResult = PronunciationAssessmentResult.FromResult(result);
                var dto = new PronunciationAssessmentDTO
                {
                    AccuracyScore = assessmentResult.AccuracyScore,
                    FluencyScore = assessmentResult.FluencyScore,
                    CompletenessScore = assessmentResult.CompletenessScore,
                    ProsodyScore = assessmentResult.ProsodyScore,
                    Words = new List<WordAssessmentDTO>()
                };

                foreach (var word in assessmentResult.Words)
                {
                    dto.Words.Add(new WordAssessmentDTO
                    {
                        Word = word.Word,
                        AccuracyScore = word.AccuracyScore,
                        ErrorType = word.ErrorType,
                        Phonemes = word.Phonemes != null ? string.Join(".", word.Phonemes.Select(p => p.Phoneme)) : ""
                    });
                }
                return dto;
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                throw new Exception("Không nhận diện được giọng nói. Vui lòng nói to, rõ hơn.");
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = CancellationDetails.FromResult(result);
                throw new Exception($"Azure Error: {cancellation.ErrorDetails}");
            }

            throw new Exception("Lỗi không xác định.");
        }

        private byte[] ConvertAudioToPcm16Khz(Stream inputStream)
        {
            try
            {
                using var reader = new StreamMediaFoundationReader(inputStream);

                var outFormat = new WaveFormat(16000, 16, 1);

                using var resampler = new MediaFoundationResampler(reader, outFormat);
                resampler.ResamplerQuality = 60; // Chất lượng cao nhất

                using var outStream = new MemoryStream();
                WaveFileWriter.WriteWavFileToStream(outStream, resampler);

                byte[] wavData = outStream.ToArray();

                if (wavData.Length <= 44) return wavData;

                return wavData.Skip(44).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audio Convert Error: {ex.Message}");
                using var ms = new MemoryStream();
                inputStream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private class BinaryAudioStreamReader : PullAudioInputStreamCallback
        {
            private readonly Stream _stream;
            public BinaryAudioStreamReader(Stream stream) { _stream = stream; }
            public override int Read(byte[] dataBuffer, uint size) { return _stream.Read(dataBuffer, 0, (int)size); }
            public override void Close() { _stream.Dispose(); }
        }
    }
}