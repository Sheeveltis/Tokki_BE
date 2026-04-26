using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.PronunciationRule.Commands.EvaluatePronunciation;
using Tokki.Application.UseCases.PronunciationRule.DTOs;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationRule
{
    public class EvaluatePronunciationCommandHandlerTests
    {
        private static EvaluatePronunciationCommandHandler CreateHandler(
            Mock<ISpeechService>?                   speechService   = null,
            Mock<IAIPronunciationService>?           aiService       = null,
            Mock<IPronunciationRuleRepository>?      ruleRepo        = null,
            Mock<IPronunciationExampleRepository>?   exampleRepo     = null,
            Mock<Hangfire.IBackgroundJobClient>?     jobClient       = null)
        {
            return new EvaluatePronunciationCommandHandler(
                (speechService  ?? new Mock<ISpeechService>()).Object,
                (aiService      ?? new Mock<IAIPronunciationService>()).Object,
                (ruleRepo       ?? MockPronunciationRuleRepository.GetMock()).Object,
                (exampleRepo    ?? MockPronunciationExampleRepository.GetMock()).Object,
                (jobClient      ?? new Mock<Hangfire.IBackgroundJobClient>()).Object);
        }

        /// <summary>Creates a mock IFormFile that returns a given stream on OpenReadStream().</summary>
        private static Mock<IFormFile> CreateMockAudioFile()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(x => x.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
            return mockFile;
        }

        /// <summary>Creates a PronunciationAssessmentDTO with specific scores.</summary>
        private static PronunciationAssessmentDTO BuildAssessment(
            double accuracy     = 85,
            double completeness = 90,
            double fluency      = 80,
            double prosody      = 75)
        {
            return new PronunciationAssessmentDTO
            {
                AccuracyScore     = accuracy,
                CompletenessScore = completeness,
                FluencyScore      = fluency,
                ProsodyScore      = prosody,
                Words             = new List<WordAssessmentDTO>()
            };
        }

        // ═══════════════════════════════════════════════════════════
        // EvaluatePronunciation_01 | A | ExampleId not found → EXAMPLE_NOT_FOUND failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExampleNotFound_ShouldReturnExampleNotFoundFailure()
        {
            // Arrange
            var exampleRepo = MockPronunciationExampleRepository.GetMock(getByIdResult: null);
            var handler = CreateHandler(exampleRepo: exampleRepo);
            var command = new EvaluatePronunciationCommand
            {
                ExampleId = "EX-NOTEXIST",
                AudioFile = CreateMockAudioFile().Object
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "EXAMPLE_NOT_FOUND");

            // Excel Log
            QACollector.LogTestCase("Pronunciation - Evaluate", new TestCaseDetail
            {
                FunctionGroup     = "EvaluatePronunciation",
                TestCaseID        = "EvaluatePronunciation_01",
                Description       = "ExampleId does not exist → EXAMPLE_NOT_FOUND failure returned",
                ExpectedResult    = "IsSuccess=false, error code EXAMPLE_NOT_FOUND",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Failure with EXAMPLE_NOT_FOUND code" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // EvaluatePronunciation_02 | A | Low scores (completeness<30 OR accuracy<20) → IsIrrelevant=true, no AI call
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_LowScores_ShouldReturnIrrelevantWithoutCallingAI()
        {
            // Arrange
            var example     = MockPronunciationExampleRepository.GetSampleExample();
            var exampleRepo = MockPronunciationExampleRepository.GetMock(getByIdResult: example);
            var lowAssessment = BuildAssessment(accuracy: 10, completeness: 20); // both below threshold

            var speechService = new Mock<ISpeechService>();
            speechService.Setup(x => x.AssessPronunciationAsync(
                    It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync(lowAssessment);

            var aiService = new Mock<IAIPronunciationService>();
            var handler = CreateHandler(speechService: speechService, aiService: aiService, exampleRepo: exampleRepo);
            var command = new EvaluatePronunciationCommand
            {
                ExampleId = example.ExampleId,
                AudioFile = CreateMockAudioFile().Object
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.IsIrrelevant.Should().BeTrue();
            aiService.Verify(x => x.GenerateFeedbackAsync(
                It.IsAny<PronunciationAssessmentDTO>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);

            // Excel Log
            QACollector.LogTestCase("Pronunciation - Evaluate", new TestCaseDetail
            {
                FunctionGroup     = "EvaluatePronunciation",
                TestCaseID        = "EvaluatePronunciation_02",
                Description       = "Azure returns low scores (accuracy<20, completeness<30) → IsIrrelevant=true, AI never called",
                ExpectedResult    = "IsSuccess=true, IsIrrelevant=true, GenerateFeedbackAsync Times.Never",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AccuracyScore=10, CompletenessScore=20", "IsIrrelevant=true", "AI skipped" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // EvaluatePronunciation_03 | N | Good scores, rule found → GenerateFeedbackAsync called with rule context
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_GoodScoresRuleFound_ShouldCallAIWithRuleContext()
        {
            // Arrange
            var rule    = MockPronunciationRuleRepository.GetSampleRule("RULE-001", "받침 발음");
            var example = MockPronunciationExampleRepository.GetSampleExample(ruleId: "RULE-001");

            var exampleRepo = MockPronunciationExampleRepository.GetMock(getByIdResult: example);
            var ruleRepo    = MockPronunciationRuleRepository.GetMock(getByIdResult: rule);
            var goodAssessment = BuildAssessment(accuracy: 85, completeness: 90);

            var speechService = new Mock<ISpeechService>();
            speechService.Setup(x => x.AssessPronunciationAsync(
                    It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync(goodAssessment);

            var aiService = new Mock<IAIPronunciationService>();
            aiService.Setup(x => x.GenerateFeedbackAsync(
                    It.IsAny<PronunciationAssessmentDTO>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(("Great job!", 88.0));

            var handler = CreateHandler(speechService: speechService, aiService: aiService,
                                        ruleRepo: ruleRepo, exampleRepo: exampleRepo);
            var command = new EvaluatePronunciationCommand
            {
                ExampleId = example.ExampleId,
                AudioFile = CreateMockAudioFile().Object
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert: AI called with context containing rule name and description
            aiService.Verify(x => x.GenerateFeedbackAsync(
                It.IsAny<PronunciationAssessmentDTO>(),
                example.RawScript,
                It.Is<string>(ctx => ctx.Contains(rule.RuleName))),
                Times.Once);

            // Excel Log
            QACollector.LogTestCase("Pronunciation - Evaluate", new TestCaseDetail
            {
                FunctionGroup     = "EvaluatePronunciation",
                TestCaseID        = "EvaluatePronunciation_03",
                Description       = "Good scores, rule found → GenerateFeedbackAsync called with ruleContext containing RuleName",
                ExpectedResult    = "GenerateFeedbackAsync(context contains RuleName) Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Good scores", "Rule found", "AI called with rule context" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // EvaluatePronunciation_04 | N | Good scores, rule NOT found → fallback context used
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_GoodScoresRuleNotFound_ShouldUseFallbackContext()
        {
            // Arrange
            var example = MockPronunciationExampleRepository.GetSampleExample(ruleId: "RULE-MISSING");
            var exampleRepo = MockPronunciationExampleRepository.GetMock(getByIdResult: example);
            var ruleRepo    = MockPronunciationRuleRepository.GetMock(getByIdResult: null); // rule not found
            var goodAssessment = BuildAssessment(accuracy: 85, completeness: 90);

            var speechService = new Mock<ISpeechService>();
            speechService.Setup(x => x.AssessPronunciationAsync(
                    It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync(goodAssessment);

            var aiService = new Mock<IAIPronunciationService>();
            aiService.Setup(x => x.GenerateFeedbackAsync(
                    It.IsAny<PronunciationAssessmentDTO>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(("Good effort!", 80.0));

            var handler = CreateHandler(speechService: speechService, aiService: aiService,
                                        ruleRepo: ruleRepo, exampleRepo: exampleRepo);
            var command = new EvaluatePronunciationCommand
            {
                ExampleId = example.ExampleId,
                AudioFile = CreateMockAudioFile().Object
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert: AI called with fallback context
            aiService.Verify(x => x.GenerateFeedbackAsync(
                It.IsAny<PronunciationAssessmentDTO>(),
                example.RawScript,
                It.Is<string>(ctx => ctx.Contains("Quy tắc phát âm cơ bản"))),
                Times.Once);

            // Excel Log
            QACollector.LogTestCase("Pronunciation - Evaluate", new TestCaseDetail
            {
                FunctionGroup     = "EvaluatePronunciation",
                TestCaseID        = "EvaluatePronunciation_04",
                Description       = "Good scores but rule not found → fallback context 'Quy tắc phát âm cơ bản' used",
                ExpectedResult    = "AI called with context='Quy tắc phát âm cơ bản'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Good scores", "GetByIdAsync(ruleId) returns null", "fallback context used" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // EvaluatePronunciation_05 | N | Happy path → IsSuccess=true, IsIrrelevant=false, 200, score from AI
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FullHappyPath_ShouldReturn200WithRelevantResponse()
        {
            // Arrange
            var rule    = MockPronunciationRuleRepository.GetSampleRule();
            var example = MockPronunciationExampleRepository.GetSampleExample(ruleId: rule.PronunciationRuleId);

            var exampleRepo = MockPronunciationExampleRepository.GetMock(getByIdResult: example);
            var ruleRepo    = MockPronunciationRuleRepository.GetMock(getByIdResult: rule);
            var goodAssessment = BuildAssessment(accuracy: 85, completeness: 92, fluency: 88, prosody: 78);

            var speechService = new Mock<ISpeechService>();
            speechService.Setup(x => x.AssessPronunciationAsync(
                    It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync(goodAssessment);

            var aiService = new Mock<IAIPronunciationService>();
            aiService.Setup(x => x.GenerateFeedbackAsync(
                    It.IsAny<PronunciationAssessmentDTO>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(("Excellent pronunciation!", 90.0));

            var handler = CreateHandler(speechService: speechService, aiService: aiService,
                                        ruleRepo: ruleRepo, exampleRepo: exampleRepo);
            var command = new EvaluatePronunciationCommand
            {
                ExampleId = example.ExampleId,
                AudioFile = CreateMockAudioFile().Object
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.IsIrrelevant.Should().BeFalse();
            result.Data!.AccuracyScore.Should().Be(90.0);
            result.Data!.AiFeedback.Should().Be("Excellent pronunciation!");
            result.Data!.FluencyScore.Should().Be(goodAssessment.FluencyScore);
            result.Data!.CompletenessScore.Should().Be(goodAssessment.CompletenessScore);

            // Excel Log
            QACollector.LogTestCase("Pronunciation - Evaluate", new TestCaseDetail
            {
                FunctionGroup     = "EvaluatePronunciation",
                TestCaseID        = "EvaluatePronunciation_05",
                Description       = "Full happy path: example found, good scores, AI returns feedback → 200 with PronunciationResponse",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, IsIrrelevant=false, AccuracyScore=90, AiFeedback set",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Example found", "Scores >threshold", "AI returns (feedback,90)", "200 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // EvaluatePronunciation_06 | A | SpeechService throws → exception propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SpeechServiceThrows_ShouldPropagateException()
        {
            // Arrange
            var example     = MockPronunciationExampleRepository.GetSampleExample();
            var exampleRepo = MockPronunciationExampleRepository.GetMock(getByIdResult: example);

            var speechService = new Mock<ISpeechService>();
            speechService.Setup(x => x.AssessPronunciationAsync(
                    It.IsAny<Stream>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Azure speech error"));

            var handler = CreateHandler(speechService: speechService, exampleRepo: exampleRepo);
            var command = new EvaluatePronunciationCommand
            {
                ExampleId = example.ExampleId,
                AudioFile = CreateMockAudioFile().Object
            };

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Azure speech error");

            // Excel Log
            QACollector.LogTestCase("Pronunciation - Evaluate", new TestCaseDetail
            {
                FunctionGroup     = "EvaluatePronunciation",
                TestCaseID        = "EvaluatePronunciation_06",
                Description       = "ISpeechService.AssessPronunciationAsync throws → exception propagates",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Example found", "SpeechService throws exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // EvaluatePronunciation_07 | A | AI service throws → exception propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AIServiceThrows_ShouldPropagateException()
        {
            // Arrange
            var example     = MockPronunciationExampleRepository.GetSampleExample();
            var exampleRepo = MockPronunciationExampleRepository.GetMock(getByIdResult: example);
            var goodAssessment = BuildAssessment(accuracy: 85, completeness: 90);

            var speechService = new Mock<ISpeechService>();
            speechService.Setup(x => x.AssessPronunciationAsync(
                    It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync(goodAssessment);

            var aiService = new Mock<IAIPronunciationService>();
            aiService.Setup(x => x.GenerateFeedbackAsync(
                    It.IsAny<PronunciationAssessmentDTO>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Gemini API error"));

            var handler = CreateHandler(speechService: speechService, aiService: aiService,
                                        exampleRepo: exampleRepo);
            var command = new EvaluatePronunciationCommand
            {
                ExampleId = example.ExampleId,
                AudioFile = CreateMockAudioFile().Object
            };

            // Act
            var act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Gemini API error");

            // Excel Log
            QACollector.LogTestCase("Pronunciation - Evaluate", new TestCaseDetail
            {
                FunctionGroup     = "EvaluatePronunciation",
                TestCaseID        = "EvaluatePronunciation_07",
                Description       = "IAIPronunciationService.GenerateFeedbackAsync throws → exception propagates",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Good scores", "AI service throws exception" }
            });
        }
    }
}