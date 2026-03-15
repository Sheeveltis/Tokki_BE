using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TextToSpeech.Commands.GenerateVocabularyAudioUrl;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.TextToSpeech
{
    public class GenerateVocabularyAudioUrlCommandHandlerTests
    {
        private GenerateVocabularyAudioUrlCommandHandler CreateHandler(
            Mock<ISpeechService>? tts = null,
            Mock<ICloudinaryService>? cloudinary = null)
        {
            return new GenerateVocabularyAudioUrlCommandHandler(
                (tts ?? MockSpeechService.GetMock()).Object,
                (cloudinary ?? MockCloudinaryService.GetMock()).Object,
                new Mock<ILogger<GenerateVocabularyAudioUrlCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_EmptyText_ShouldReturn400()
        {
            var command = new GenerateVocabularyAudioUrlCommand { Text = "" };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("TTS - Generate Audio URL", new TestCaseDetail
            {
                FunctionGroup = "Generate Vocabulary Audio URL",
                TestCaseID = "TC-TTS-GEN-01",
                Description = "Text rỗng → return 400 TTS.InvalidText",
                ExpectedResult = "Return 400 Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Text = empty string",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidText_ShouldReturnAudioUrl()
        {
            var command = new GenerateVocabularyAudioUrlCommand { Text = "안녕하세요" };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.AudioUrl.Should().NotBeNullOrEmpty();

            QACollector.LogTestCase("TTS - Generate Audio URL", new TestCaseDetail
            {
                FunctionGroup = "Generate Vocabulary Audio URL",
                TestCaseID = "TC-TTS-GEN-02",
                Description = "Text hợp lệ → TTS tổng hợp âm thanh, upload Cloudinary, trả về URL",
                ExpectedResult = "Return 200, AudioUrl != null",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid text",
                    "TTS success",
                    "Cloudinary upload success",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_TtsThrowsException_ShouldReturn500()
        {
            var command = new GenerateVocabularyAudioUrlCommand { Text = "안녕" };

            var mockTts = MockSpeechService.GetMock();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ThrowsAsync(new Exception("TTS service unavailable"));

            var handler = CreateHandler(tts: mockTts);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("TTS - Generate Audio URL", new TestCaseDetail
            {
                FunctionGroup = "Generate Vocabulary Audio URL",
                TestCaseID = "TC-TTS-GEN-03",
                Description = "TTS service throw exception → return 500",
                ExpectedResult = "Return 500 ServerError",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "TTS throws Exception",
                    "Caught in try/catch",
                    "Return 500"
                }
            });
        }
    }
}