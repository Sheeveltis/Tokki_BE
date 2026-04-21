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

            pronunciationConfig.EnableProsodyAssessment();

            var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1);

            using var convertedStream = new MemoryStream(pcmData);
            using var reader = new BinaryAudioStreamReader(convertedStream);
            using var audioInputStream = AudioInputStream.CreatePullStream(reader, audioFormat);
            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);

            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            var phraseList = PhraseListGrammar.FromRecognizer(recognizer);
            phraseList.AddPhrase(referenceText);
            foreach (var w in referenceText.Split(new[] { ' ', '.', ',', '?', '!' }, StringSplitOptions.RemoveEmptyEntries))
            {
                phraseList.AddPhrase(w);
            }

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
                    var wordDto = new WordAssessmentDTO
                    {
                        Word = word.Word,
                        AccuracyScore = word.AccuracyScore,
                        ErrorType = word.ErrorType,
                        Phonemes = word.Syllables != null
                            ? string.Join(".", word.Syllables.Select(s => s.AccuracyScore))
                            : string.Empty
                    };

                    if (word.Syllables != null)
                    {
                        foreach (var s in word.Syllables)
                        {
                            wordDto.Syllables.Add(new SyllableAssessmentDTO
                            {
                                Syllable = s.Syllable,
                                AccuracyScore = s.AccuracyScore
                            });
                        }
                    }

                    dto.Words.Add(wordDto);
                }
                return dto;
            }

            throw result.Reason switch
            {
                ResultReason.NoMatch => new Exception("Không nhận diện được giọng nói. Vui lòng nói rõ hơn."),
                ResultReason.Canceled => new Exception($"Azure Error: {CancellationDetails.FromResult(result).ErrorDetails}"),
                _ => new Exception("Lỗi không xác định khi gọi Azure Speech.")
            };
        }

        private class BinaryAudioStreamReader : PullAudioInputStreamCallback
        {
            private readonly Stream _stream;
            public BinaryAudioStreamReader(Stream stream) { _stream = stream; }
            public override int Read(byte[] dataBuffer, uint size) { return _stream.Read(dataBuffer, 0, (int)size); }
            public override void Close() { _stream.Dispose(); }
        }
        private byte[] ConvertAudioToPcm16Khz(Stream inputStream)
        {
            try
            {
                var inputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp");
                var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");

                try
                {
                    using (var fs = File.Create(inputPath))
                        inputStream.CopyTo(fs);

                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "ffmpeg",
                            Arguments = $"-y -i \"{inputPath}\" -ar 16000 -ac 1 -sample_fmt s16 \"{outputPath}\"",
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    process.WaitForExit(30000);

                    if (process.ExitCode != 0)
                    {
                        var err = process.StandardError.ReadToEnd();
                        throw new Exception($"ffmpeg error: {err}");
                    }

                    byte[] wavData = File.ReadAllBytes(outputPath);
                    return wavData.Length > 44 ? wavData.Skip(44).ToArray() : wavData;
                }
                finally
                {
                    if (File.Exists(inputPath)) File.Delete(inputPath);
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audio Convert Error: {ex.Message}");
                using var ms = new MemoryStream();
                inputStream.Position = 0;
                inputStream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}