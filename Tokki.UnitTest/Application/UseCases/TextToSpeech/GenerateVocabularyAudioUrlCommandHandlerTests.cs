using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TextToSpeech.Commands.GenerateVocabularyAudioUrl;
using Tokki.Application.UseCases.TextToSpeech.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.TextToSpeech
{
    public class GenerateVocabularyAudioUrlCommandHandlerTests
    {
        private static Mock<ISpeechService> GetSpeechMock(byte[]? bytes = null)
        {
            var m = new Mock<ISpeechService>();
            m.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
             .ReturnsAsync(bytes ?? new byte[] { 1, 2, 3 });
            return m;
        }

        private static Mock<ICloudinaryService> GetCloudinaryMock(string url = "https://res.cloudinary.com/vocab-audio/abc.mp3")
        {
            var m = new Mock<ICloudinaryService>();
            m.Setup(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(url);
            return m;
        }

        private static GenerateVocabularyAudioUrlCommandHandler CreateHandler(
            Mock<ISpeechService>?    speech     = null,
            Mock<ICloudinaryService>? cloudinary = null)
            => new GenerateVocabularyAudioUrlCommandHandler(
                (speech     ?? GetSpeechMock()).Object,
                (cloudinary ?? GetCloudinaryMock()).Object,
                NullLogger<GenerateVocabularyAudioUrlCommandHandler>.Instance);

        // TC-TTS-01 | A | Text is null/empty → 400 failure
        [Fact]
        public async Task Handle_EmptyText_ShouldReturn400Failure()
        {
            var result = await CreateHandler().Handle(new GenerateVocabularyAudioUrlCommand { Text = "" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("TextToSpeech - Generate Audio URL", new TestCaseDetail { FunctionGroup = "GenerateVocabularyAudioUrl", TestCaseID = "TC-TTS-01", Description = "Empty text → 400, failure", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Text is empty string" } });
        }

        // TC-TTS-02 | A | Text is whitespace → 400 failure
        [Fact]
        public async Task Handle_WhitespaceText_ShouldReturn400Failure()
        {
            var result = await CreateHandler().Handle(new GenerateVocabularyAudioUrlCommand { Text = "   " }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("TextToSpeech - Generate Audio URL", new TestCaseDetail { FunctionGroup = "GenerateVocabularyAudioUrl", TestCaseID = "TC-TTS-02", Description = "Whitespace text → 400, failure", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Text is all whitespace" } });
        }

        // TC-TTS-03 | N | Happy path: valid text → 200 with AudioUrl
        [Fact]
        public async Task Handle_ValidText_ShouldReturn200WithAudioUrl()
        {
            var cloudinary = GetCloudinaryMock("https://cdn.example.com/audio.mp3");
            var result     = await CreateHandler(cloudinary: cloudinary).Handle(new GenerateVocabularyAudioUrlCommand { Text = "사랑" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.AudioUrl.Should().Be("https://cdn.example.com/audio.mp3");
            QACollector.LogTestCase("TextToSpeech - Generate Audio URL", new TestCaseDetail { FunctionGroup = "GenerateVocabularyAudioUrl", TestCaseID = "TC-TTS-03", Description = "Valid text '사랑' → 200, AudioUrl returned", ExpectedResult = "IsSuccess=true, 200, Data.AudioUrl set", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TTS + Cloudinary succeed" } });
        }

        // TC-TTS-04 | B | SynthesizeKoreanAudioAsync called with trimmed text
        [Fact]
        public async Task Handle_TextWithSpaces_SpeechCalledWithTrimmedText()
        {
            var speech = GetSpeechMock();
            await CreateHandler(speech: speech).Handle(new GenerateVocabularyAudioUrlCommand { Text = "  안녕  " }, CancellationToken.None);
            speech.Verify(x => x.SynthesizeKoreanAudioAsync("안녕"), Times.Once);
            QACollector.LogTestCase("TextToSpeech - Generate Audio URL", new TestCaseDetail { FunctionGroup = "GenerateVocabularyAudioUrl", TestCaseID = "TC-TTS-04", Description = "Text '  안녕  ' trimmed before TTS call", ExpectedResult = "SynthesizeKoreanAudioAsync called with '안녕'", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Text.Trim() applied" } });
        }

        // TC-TTS-05 | B | UploadAudioAsync called with correct folder
        [Fact]
        public async Task Handle_ValidText_UploadCalledWithCorrectFolder()
        {
            var cloudinary = GetCloudinaryMock();
            await CreateHandler(cloudinary: cloudinary).Handle(new GenerateVocabularyAudioUrlCommand { Text = "테스트" }, CancellationToken.None);
            cloudinary.Verify(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), "tokki/vocab-audio"), Times.Once);
            QACollector.LogTestCase("TextToSpeech - Generate Audio URL", new TestCaseDetail { FunctionGroup = "GenerateVocabularyAudioUrl", TestCaseID = "TC-TTS-05", Description = "UploadAudioAsync called with folder='tokki/vocab-audio'", ExpectedResult = "Times.Once with correct folder", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Folder name hardcoded to 'tokki/vocab-audio'" } });
        }

        // TC-TTS-06 | A | TTS service throws → 500 failure
        [Fact]
        public async Task Handle_SpeechServiceThrows_ShouldReturn500Failure()
        {
            var speech = new Mock<ISpeechService>();
            speech.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>())).ThrowsAsync(new Exception("TTS error"));
            var result = await CreateHandler(speech: speech).Handle(new GenerateVocabularyAudioUrlCommand { Text = "학교" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            QACollector.LogTestCase("TextToSpeech - Generate Audio URL", new TestCaseDetail { FunctionGroup = "GenerateVocabularyAudioUrl", TestCaseID = "TC-TTS-06", Description = "TTS service throws → 500 failure", ExpectedResult = "IsSuccess=false, 500", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Exception caught, 500 returned" } });
        }
    }
}