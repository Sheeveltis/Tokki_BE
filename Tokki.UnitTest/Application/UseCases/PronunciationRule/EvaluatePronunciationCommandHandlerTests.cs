using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.PronunciationRule.Commands.EvaluatePronunciation;
using Tokki.Application.UseCases.PronunciationRule.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationRule
{
    public class EvaluatePronunciationCommandHandlerTests
    {
        private static IFormFile CreateFakeAudio()
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(x => x.OpenReadStream())
                .Returns(new MemoryStream(new byte[] { 0x01 }));
            return mock.Object;
        }

        private EvaluatePronunciationCommandHandler CreateHandler(
            Mock<ISpeechService>? speechService = null,
            Mock<IAIPronunciationService>? aiService = null,
            Mock<IPronunciationRuleRepository>? ruleRepo = null,
            Mock<IPronunciationExampleRepository>? exampleRepo = null)
        {
            return new EvaluatePronunciationCommandHandler(
                (speechService ?? new Mock<ISpeechService>()).Object,
                (aiService ?? new Mock<IAIPronunciationService>()).Object,
                (ruleRepo ?? new Mock<IPronunciationRuleRepository>()).Object,
                (exampleRepo ?? new Mock<IPronunciationExampleRepository>()).Object);
        }

        [Fact]
        public async Task Handle_ExampleNotFound_ShouldReturnFailure()
        {
            // Arrange
            var command = new EvaluatePronunciationCommand
            {
                ExampleId = "EX-INVALID",
                AudioFile = CreateFakeAudio()
            };

            var mockExampleRepo = new Mock<IPronunciationExampleRepository>();
            mockExampleRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                           .ReturnsAsync((Domain.Entities.PronunciationExample?)null);

            var handler = CreateHandler(exampleRepo: mockExampleRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("PronunciationRule - Evaluate", new TestCaseDetail
            {
                FunctionGroup = "Evaluate Pronunciation",
                TestCaseID = "TC-PRN-EVL-01",
                Description = "Evaluate với ExampleId không tồn tại",
                ExpectedResult = "Return Failure EXAMPLE_NOT_FOUND",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid ExampleId",
                    "Example = null",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_LowScores_ShouldReturnSuccessWithDefaultFeedback()
        {
            // Arrange - CompletenessScore < 30 → trả về feedback mặc định
            var command = new EvaluatePronunciationCommand
            {
                ExampleId = "EX-001",
                AudioFile = CreateFakeAudio()
            };

            var fakeExample = new Domain.Entities.PronunciationExample
            {
                ExampleId = "EX-001",
                RawScript = "안녕하세요",
                PronunciationRuleId = "RULE-001"
            };

            var mockExampleRepo = new Mock<IPronunciationExampleRepository>();
            mockExampleRepo.Setup(x => x.GetByIdAsync("EX-001"))
                           .ReturnsAsync(fakeExample);

            var mockSpeech = new Mock<ISpeechService>();
            mockSpeech.Setup(x => x.AssessPronunciationAsync(
                        It.IsAny<Stream>(),
                        It.IsAny<string>()))
                      .ReturnsAsync(new PronunciationResponse
                      {
                          AccuracyScore = 15,       // < 20
                          CompletenessScore = 20,   // < 30
                          FluencyScore = 50,
                          ProsodyScore = 50
                      });

            var handler = CreateHandler(
                speechService: mockSpeech,
                exampleRepo: mockExampleRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.AiFeedback.Should().Contain("chưa đúng");

            QACollector.LogTestCase("PronunciationRule - Evaluate", new TestCaseDetail
            {
                FunctionGroup = "Evaluate Pronunciation",
                TestCaseID = "TC-PRN-EVL-02",
                Description = "Score thấp (CompletenessScore < 30) → trả về feedback mặc định",
                ExpectedResult = "Return Success với AiFeedback mặc định",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "CompletenessScore = 20 (boundary: < 30)",
                    "AccuracyScore = 15 (boundary: < 20)",
                    "Trả về default feedback"
                }
            });
        }

        [Fact]
        public async Task Handle_GoodScores_ShouldCallAIAndReturnFeedback()
        {
            // Arrange - Score đủ tốt → gọi AI service
            var command = new EvaluatePronunciationCommand
            {
                ExampleId = "EX-001",
                AudioFile = CreateFakeAudio()
            };

            var fakeExample = new Domain.Entities.PronunciationExample
            {
                ExampleId = "EX-001",
                RawScript = "안녕하세요",
                PronunciationRuleId = "RULE-001"
            };

            var mockExampleRepo = new Mock<IPronunciationExampleRepository>();
            mockExampleRepo.Setup(x => x.GetByIdAsync("EX-001"))
                           .ReturnsAsync(fakeExample);

            var mockRuleRepo = new Mock<IPronunciationRuleRepository>();
            mockRuleRepo.Setup(x => x.GetByIdAsync("RULE-001"))
                        .ReturnsAsync((Domain.Entities.PronunciationRule?)null);

            var mockSpeech = new Mock<ISpeechService>();
            mockSpeech.Setup(x => x.AssessPronunciationAsync(
                        It.IsAny<Stream>(),
                        It.IsAny<string>()))
                      .ReturnsAsync(new PronunciationResponse
                      {
                          AccuracyScore = 85,
                          CompletenessScore = 90,
                          FluencyScore = 80,
                          ProsodyScore = 75
                      });

            var mockAI = new Mock<IAIPronunciationService>();
            mockAI.Setup(x => x.GenerateFeedbackAsync(
                        It.IsAny<PronunciationResponse>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                  .ReturnsAsync(("Phát âm tốt!", 88.0));

            var handler = CreateHandler(
                speechService: mockSpeech,
                aiService: mockAI,
                ruleRepo: mockRuleRepo,
                exampleRepo: mockExampleRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.AiFeedback.Should().Be("Phát âm tốt!");
            result.Data.AccuracyScore.Should().Be(88.0);

            mockAI.Verify(x => x.GenerateFeedbackAsync(
                It.IsAny<PronunciationResponse>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);

            QACollector.LogTestCase("PronunciationRule - Evaluate", new TestCaseDetail
            {
                FunctionGroup = "Evaluate Pronunciation",
                TestCaseID = "TC-PRN-EVL-03",
                Description = "Score đủ tốt → gọi AI service và trả về feedback từ AI",
                ExpectedResult = "AI called once, AiFeedback = 'Phát âm tốt!'",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "CompletenessScore = 90 (>= 30)",
                    "AccuracyScore = 85 (>= 20)",
                    "AI service called once",
                    "Return Success"
                }
            });
        }
    }
}