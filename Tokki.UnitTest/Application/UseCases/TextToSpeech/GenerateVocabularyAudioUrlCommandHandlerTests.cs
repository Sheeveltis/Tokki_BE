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

        // GenerateVocabularyAudioUrl_01 | A | Text is null/empty ? 400 failure
        [Fact]
        public async Task Handle_EmptyText_ShouldReturn400Failure()
        {
            var result = await CreateHandler().Handle(new GenerateVocabularyAudioUrlCommand { Text = "" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("TextToSpeech - Generate Audio URL", new TestCaseDetail { FunctionGroup = "GenerateVocabularyAudioUrl", TestCaseID = "GenerateVocabularyAudioUrl_01", Description = "Empty text ? 400, failure", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Text is empty string" } });
        }

        // GenerateVocabularyAudioUrl_02 | A | Text is whitespace ? 400 failure
        [Fact]
        public async Task Handle_WhitespaceText_ShouldReturn400Failure()
        {
            var result = await CreateHandler().Handle(new GenerateVocabularyAudioUrlCommand { Text = "   " }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("TextToSpeech - Generate Audio URL", new TestCaseDetail { FunctionGroup = "GenerateVocabularyAudioUrl", TestCaseID = "GenerateVocabularyAudioUrl_02", Description = "Whitespace text ? 400, failure", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Text is all whitespace" } });
        }

        // GenerateVocabularyAudioUrl_03 | N | Happy path: valid text ? 200 with AudioUrl
        [Fact]
        public async Task Handle_ValidText_ShouldReturn200WithAudioUrl()
        {
            var cloudinary = GetCloudinaryMock("https://cdn.example.com/audio.mp3");
            var result     = await CreateHandler(cloudinary: cloudinary).Handle(new GenerateVocabularyAudioUrlCommand { Text = "??" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.AudioUrl.Should().Be("https://cdn.example.com/audio.mp3");
            QACollector.LogTestCase("TextToSpeech - Generate Audio URL", new TestCaseDetail { FunctionGroup = "GenerateVocabularyAudioUrl", TestCaseID = "GenerateVocabularyAudioUrl_03", Description = "Valid text '??' ? 200, AudioUrl returned", ExpectedResult = "IsSuccess=true, 200, Data.AudioUrl set", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TTS + Cloudinary succeed" } });
        }

        // GenerateVocabularyAudioUrl_04 | B | SynthesizeKoreanAudioAsync called with trimmed text
        [Fact]
        public async Task Handle_TextWithSpaces_SpeechCalledWithTrimmedText()
        {
            var speech = GetSpeechMock();
            await CreateHandler(speech: speech).Handle(new GenerateVocabularyAudioUrlCommand { Text = "  ??" }, CancellationToken.None);
            speech.Verify(x => x.SynthesizeKoreanAudioAsync("??"), Times.Once);
            QACollector.LogTestCase("TextToSpeech - Generate Audio URL", new TestCaseDetail { FunctionGroup = "GenerateVocabularyAudioUrl", TestCaseID = "GenerateVocabularyAudioUrl_04", Description = "Text '  ??  ' trimmed before TTS call", ExpectedResult = "SynthesizeKoreanAudioAsync called with '??'", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Text.Trim() applied" } });
        }

        // GenerateVocabularyAudioUrl_05 | B | UploadAudioAsync called with correct folder
        [Fact]
        public async Task Handle_ValidText_UploadCalledWithCorrectFolder()
        {
            var cloudinary = GetCloudinaryMock();
            await CreateHandler(cloudinary: cloudinary).Handle(new GenerateVocabularyAudioUrlCommand { Text = "???" }, CancellationToken.None);
            cloudinary.Verify(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), "tokki/vocab-audio"), Times.Once);
            QACollector.LogTestCase("TextToSpeech - Generate Audio URL", new TestCaseDetail { FunctionGroup = "GenerateVocabularyAudioUrl", TestCaseID = "GenerateVocabularyAudioUrl_05", Description = "UploadAudioAsync called with folder='tokki/vocab-audio'", ExpectedResult = "Times.Once with correct folder", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Folder name hardcoded to 'tokki/vocab-audio'" } });
        }

        // GenerateVocabularyAudioUrl_06 | A | TTS service throws ? 500 failure
        [Fact]
        public async Task Handle_SpeechServiceThrows_ShouldReturn500Failure()
        {
            var speech = new Mock<ISpeechService>();
            speech.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>())).ThrowsAsync(new Exception("TTS error"));
            var result = await CreateHandler(speech: speech).Handle(new GenerateVocabularyAudioUrlCommand { Text = "??" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            QACollector.LogTestCase("TextToSpeech - Generate Audio URL", new TestCaseDetail { FunctionGroup = "GenerateVocabularyAudioUrl", TestCaseID = "GenerateVocabularyAudioUrl_06", Description = "TTS service throws ? 500 failure", ExpectedResult = "IsSuccess=false, 500", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Exception caught, 500 returned" } });
        }
    }
}