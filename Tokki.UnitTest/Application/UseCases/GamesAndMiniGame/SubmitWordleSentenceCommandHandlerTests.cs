using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.MiniGame.Commands.SubmitWordleSentence;
using Tokki.Application.UseCases.MiniGame.DTOs;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.GamesAndMiniGame
{
    public class SubmitWordleSentenceCommandHandlerTests
    {
        private SubmitWordleSentenceCommandHandler CreateHandler(
            Mock<IWordleRepository>? wordleRepo = null,
            Mock<IAIWordleService>? aiService = null)
        {
            return new SubmitWordleSentenceCommandHandler(
                (wordleRepo ?? new Mock<IWordleRepository>()).Object,
                (aiService ?? new Mock<IAIWordleService>()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        private static WordleAiFeedbackDto CreateFakeAiFeedback(double score = 80.0)
        {
            return new WordleAiFeedbackDto
            {
                ContainsTargetWord = true,
                TotalScore         = score,
                GeneralFeedback    = "Good sentence!",
                Meaning            = new CriterionDto { Score = 30, MaxScore = 40, Feedback = "OK" },
                Grammar            = new CriterionDto { Score = 25, MaxScore = 30, Feedback = "OK" },
                Naturalness        = new CriterionDto { Score = 25, MaxScore = 30, Feedback = "OK" }
            };
        }

        [Fact]
        public async Task Handle_ValidSentence_ShouldCallAIAndReturnSubmission()
        {
            var command = new SubmitWordleSentenceCommand
            {
                UserId          = "USER-001",
                DailyWordleId   = "WORDLE-001",
                SentenceContent = "저는 학생입니다."
            };

            var vocab = new Tokki.Domain.Entities.Vocabulary { VocabularyId = "VOCAB-001", Text = "학생", Definition = "Pupil" };
            var dailyWordle = new DailyWordle { DailyWordleId = "WORDLE-001", VocabularyId = "VOCAB-001", Vocabulary = vocab };
            var aiFeedback = CreateFakeAiFeedback(85.5);

            var mockRepo = new Mock<IWordleRepository>();
            mockRepo.Setup(x => x.GetDailyWordleWithVocabAsync("WORDLE-001", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dailyWordle);
            mockRepo.Setup(x => x.AddSubmissionAsync(It.IsAny<WordleSentenceSubmission>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var mockAI = new Mock<IAIWordleService>();
            mockAI.Setup(x => x.EvaluateSentenceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(aiFeedback);

            var result = await CreateHandler(wordleRepo: mockRepo, aiService: mockAI).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.TargetWord.Should().Be("학생");
            result.Data.Meaning.Should().Be("Pupil");
            result.Data.AiFeedback.Should().BeEquivalentTo(aiFeedback);
            result.Data.SubmissionId.Should().NotBeNullOrEmpty();
            mockAI.Verify(x => x.EvaluateSentenceAsync("저는 학생입니다.", "학생", "Pupil"), Times.Once);

            QACollector.LogTestCase("Wordle - Submit Sentence", new TestCaseDetail
            {
                FunctionGroup     = "Submit Wordle Sentence",
                TestCaseID        = "TC-WDL-SEN-01",
                Description       = "Submit valid sentence → AI evaluates, saves submission, returns response",
                ExpectedResult    = "Return Success, TargetWord='학생', AI called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid DailyWordleId", "AI called once", "Submission saved" }
            });
        }

        [Fact]
        public async Task Handle_ValidSentence_AiScoreShouldBeRounded()
        {
            var command = new SubmitWordleSentenceCommand
            {
                UserId          = "USER-001",
                DailyWordleId   = "WORDLE-001",
                SentenceContent = "안녕하세요."
            };

            var dailyWordle = new DailyWordle
            {
                DailyWordleId = "WORDLE-001",
                Vocabulary    = new Tokki.Domain.Entities.Vocabulary { Text = "안녕", Definition = "Hello" }
            };

            WordleSentenceSubmission? capturedSubmission = null;

            var mockRepo = new Mock<IWordleRepository>();
            mockRepo.Setup(x => x.GetDailyWordleWithVocabAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dailyWordle);
            mockRepo.Setup(x => x.AddSubmissionAsync(It.IsAny<WordleSentenceSubmission>(), It.IsAny<CancellationToken>()))
                    .Callback<WordleSentenceSubmission, CancellationToken>((s, _) => capturedSubmission = s)
                    .Returns(Task.CompletedTask);

            var mockAI = new Mock<IAIWordleService>();
            mockAI.Setup(x => x.EvaluateSentenceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(CreateFakeAiFeedback(85.6));

            var result = await CreateHandler(wordleRepo: mockRepo, aiService: mockAI).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            capturedSubmission.Should().NotBeNull();
            capturedSubmission!.AiScore.Should().Be(86);

            QACollector.LogTestCase("Wordle - Submit Sentence", new TestCaseDetail
            {
                FunctionGroup     = "Submit Wordle Sentence",
                TestCaseID        = "TC-WDL-SEN-02",
                Description       = "TotalScore = 85.6 → AiScore is rounded to 86",
                ExpectedResult    = "AiScore = 86 (Math.Round)",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TotalScore = 85.6 => AiScore = Math.Round(85.6) = 86" }
            });
        }

        [Fact]
        public async Task Handle_DailyWordleNotFound_ShouldReturn404()
        {
            var command = new SubmitWordleSentenceCommand
            {
                UserId          = "USER-001",
                DailyWordleId   = "WORDLE-INVALID",
                SentenceContent = "테스트 문장."
            };

            var mockRepo = new Mock<IWordleRepository>();
            mockRepo.Setup(x => x.GetDailyWordleWithVocabAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((DailyWordle?)null);

            var result = await CreateHandler(wordleRepo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Wordle - Submit Sentence", new TestCaseDetail
            {
                FunctionGroup     = "Submit Wordle Sentence",
                TestCaseID        = "TC-WDL-SEN-03",
                Description       = "DailyWordleId does not exist → return 404",
                ExpectedResult    = "Return 404 Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DailyWordle = null => Return 404" }
            });
        }

        [Fact]
        public async Task Handle_DailyWordleExistsButVocabularyNull_ShouldReturn404()
        {
            var command = new SubmitWordleSentenceCommand
            {
                UserId          = "USER-001",
                DailyWordleId   = "WORDLE-001",
                SentenceContent = "테스트."
            };

            var mockRepo = new Mock<IWordleRepository>();
            mockRepo.Setup(x => x.GetDailyWordleWithVocabAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DailyWordle { DailyWordleId = "WORDLE-001", Vocabulary = null });

            var result = await CreateHandler(wordleRepo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Wordle - Submit Sentence", new TestCaseDetail
            {
                FunctionGroup     = "Submit Wordle Sentence",
                TestCaseID        = "TC-WDL-SEN-04",
                Description       = "DailyWordle exists but Vocabulary = null → return 404",
                ExpectedResult    = "Return 404 Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DailyWordle.Vocabulary = null => Return 404" }
            });
        }

        [Fact]
        public async Task Handle_AIServiceThrows_ShouldPropagateException()
        {
            var command = new SubmitWordleSentenceCommand
            {
                UserId          = "USER-001",
                DailyWordleId   = "WORDLE-001",
                SentenceContent = "테스트."
            };

            var mockRepo = new Mock<IWordleRepository>();
            mockRepo.Setup(x => x.GetDailyWordleWithVocabAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DailyWordle
                    {
                        DailyWordleId = "WORDLE-001",
                        Vocabulary    = new Tokki.Domain.Entities.Vocabulary { Text = "테스트", Definition = "Test" }
                    });

            var mockAI = new Mock<IAIWordleService>();
            mockAI.Setup(x => x.EvaluateSentenceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .ThrowsAsync(new Exception("AI service timeout"));

            await ((Func<Task>)(async () =>
     await CreateHandler(wordleRepo: mockRepo, aiService: mockAI).Handle(command, CancellationToken.None)))
     .Should().ThrowAsync<Exception>()
     .WithMessage("AI service timeout");
            QACollector.LogTestCase("Wordle - Submit Sentence", new TestCaseDetail
            {
                FunctionGroup     = "Submit Wordle Sentence",
                TestCaseID        = "TC-WDL-SEN-05",
                Description       = "AI service throws exception → exception propagates",
                ExpectedResult    = "Throws Exception with message 'AI service timeout'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "EvaluateSentenceAsync throws" }
            });
        }

        [Fact]
        public async Task Handle_ValidSentence_SubmissionIdGenerated()
        {
            var command = new SubmitWordleSentenceCommand
            {
                UserId          = "USER-001",
                DailyWordleId   = "WORDLE-001",
                SentenceContent = "나는 학생이에요."
            };

            var vocab = new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V1", Text = "학생", Definition = "Student" };
            var dailyWordle = new DailyWordle { DailyWordleId = "WORDLE-001", Vocabulary = vocab };

            var mockRepo = new Mock<IWordleRepository>();
            mockRepo.Setup(x => x.GetDailyWordleWithVocabAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dailyWordle);
            mockRepo.Setup(x => x.AddSubmissionAsync(It.IsAny<WordleSentenceSubmission>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var mockAI = new Mock<IAIWordleService>();
            mockAI.Setup(x => x.EvaluateSentenceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(CreateFakeAiFeedback(70.0));

            var result = await CreateHandler(wordleRepo: mockRepo, aiService: mockAI).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.SubmissionId.Should().NotBeNullOrWhiteSpace();
            mockRepo.Verify(x => x.AddSubmissionAsync(It.IsAny<WordleSentenceSubmission>(), It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Wordle - Submit Sentence", new TestCaseDetail
            {
                FunctionGroup     = "Submit Wordle Sentence",
                TestCaseID        = "TC-WDL-SEN-06",
                Description       = "Successfully submitted → SubmissionId generated and not empty",
                ExpectedResult    = "Return 200, SubmissionId != null/empty, AddSubmissionAsync called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Result.SubmissionId = idGenerator.Generate()" }
            });
        }
    }
}
