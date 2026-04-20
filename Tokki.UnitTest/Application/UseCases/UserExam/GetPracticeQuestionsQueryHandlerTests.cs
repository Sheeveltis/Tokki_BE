using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Application.UseCases.UserExam.Queries.GetPracticeQuestions;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class GetPracticeQuestionsQueryHandlerTests
    {
        private static GetPracticeQuestionsQueryHandler CreateHandler(
            Mock<IQuestionBankRepository>? qbRepo     = null,
            Mock<IPassageRepository>?      passageRepo = null)
            => new GetPracticeQuestionsQueryHandler(
                (qbRepo      ?? new Mock<IQuestionBankRepository>()).Object,
                (passageRepo ?? new Mock<IPassageRepository>()).Object);

        private static List<QuestionBank> BuildQuestions(int count = 2)
        {
            var list = new List<QuestionBank>();
            for (int i = 1; i <= count; i++)
            {
                list.Add(new QuestionBank
                {
                    QuestionBankId  = $"QB-{i:000}",
                    Content         = $"Question {i}",
                    PassageId       = null,
                    MediaUrl        = null,
                    Explanation     = $"Explanation {i}",
                    QuestionOptions = new List<QuestionOption>
                    {
                        new QuestionOption { OptionId = $"OPT-A{i}", IsCorrect = true,  Content = "A" },
                        new QuestionOption { OptionId = $"OPT-B{i}", IsCorrect = false, Content = "B" }
                    }
                });
            }
            return list;
        }

        // ═══════════════════════════════════════════════════════════════
        // GetPracticeQuestions_01 | A | No questions found for the type → 404
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoQuestionsFound_ShouldReturn404()
        {
            // Arrange
            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetRandomQuestionsForPracticeAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<QuestionBank>());
            var handler = CreateHandler(qbRepo);

            // Act
            var result = await handler.Handle(
                new GetPracticeQuestionsQuery { QuestionTypeId = "QT-INVALID", Quantity = 5 },
                CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Practice Questions", new TestCaseDetail
            {
                FunctionGroup     = "GetPracticeQuestions",
                TestCaseID        = "GetPracticeQuestions_01",
                Description       = "No questions for the requested QuestionTypeId → 404",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetRandomQuestionsForPracticeAsync returns empty list" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetPracticeQuestions_02 | A | Repository returns null → 404
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullReturnedFromRepo_ShouldReturn404()
        {
            // Arrange
            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetRandomQuestionsForPracticeAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((List<QuestionBank>?)null!);
            var handler = CreateHandler(qbRepo);

            // Act
            var result = await handler.Handle(
                new GetPracticeQuestionsQuery { QuestionTypeId = "QT-001", Quantity = 5 },
                CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Practice Questions", new TestCaseDetail
            {
                FunctionGroup     = "GetPracticeQuestions",
                TestCaseID        = "GetPracticeQuestions_02",
                Description       = "Repository returns null → treated as 404",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetRandomQuestionsForPracticeAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetPracticeQuestions_03 | N | 2 questions returned → 2 groups (no shared passage)
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TwoQuestionsNoPassage_ShouldReturnTwoGroups()
        {
            // Arrange
            var questions = BuildQuestions(2);
            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetRandomQuestionsForPracticeAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(questions);
            var passageRepo = new Mock<IPassageRepository>();
            passageRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<Passage>());
            var handler = CreateHandler(qbRepo, passageRepo);

            // Act
            var result = await handler.Handle(
                new GetPracticeQuestionsQuery { QuestionTypeId = "QT-001", Quantity = 2 },
                CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Should().HaveCount(2); // each question in its own group

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Practice Questions", new TestCaseDetail
            {
                FunctionGroup     = "GetPracticeQuestions",
                TestCaseID        = "GetPracticeQuestions_03",
                Description       = "2 stand-alone questions → 2 separate groups",
                ExpectedResult    = "IsSuccess=true, Data.Count=2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No shared passage, no shared media" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetPracticeQuestions_04 | N | Correct option included in each group's questions
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidQuestions_ShouldIncludeCorrectOptionIdInResult()
        {
            // Arrange
            var questions = BuildQuestions(1);
            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetRandomQuestionsForPracticeAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(questions);
            var handler = CreateHandler(qbRepo);

            // Act
            var result = await handler.Handle(
                new GetPracticeQuestionsQuery { QuestionTypeId = "QT-001", Quantity = 1 },
                CancellationToken.None);

            // Assert
            var q = result.Data![0].Questions[0];
            q.CorrectOptionId.Should().Be("OPT-A1");

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Practice Questions", new TestCaseDetail
            {
                FunctionGroup     = "GetPracticeQuestions",
                TestCaseID        = "GetPracticeQuestions_04",
                Description       = "Practice question includes CorrectOptionId from the IsCorrect option",
                ExpectedResult    = "CorrectOptionId=OPT-A1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsCorrect=true option mapped to CorrectOptionId" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetPracticeQuestions_05 | N | Quantity forwarded to repository correctly
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ShouldForwardQuantityToRepo()
        {
            // Arrange
            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetRandomQuestionsForPracticeAsync(
                    "QT-999", 15, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(BuildQuestions(2));
            var handler = CreateHandler(qbRepo);

            // Act
            await handler.Handle(
                new GetPracticeQuestionsQuery { QuestionTypeId = "QT-999", Quantity = 15 },
                CancellationToken.None);

            // Assert
            qbRepo.Verify(x => x.GetRandomQuestionsForPracticeAsync("QT-999", 15, It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Practice Questions", new TestCaseDetail
            {
                FunctionGroup     = "GetPracticeQuestions",
                TestCaseID        = "GetPracticeQuestions_05",
                Description       = "QuestionTypeId and Quantity forwarded to repo correctly",
                ExpectedResult    = "GetRandomQuestionsForPracticeAsync called with QT-999 and 15",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionTypeId='QT-999', Quantity=15" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetPracticeQuestions_06 | E | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetRandomQuestionsForPracticeAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("DB failure"));
            var handler = CreateHandler(qbRepo);

            // Act
            var act = async () => await handler.Handle(
                new GetPracticeQuestionsQuery { QuestionTypeId = "QT-001", Quantity = 5 },
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB failure");

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Practice Questions", new TestCaseDetail
            {
                FunctionGroup     = "GetPracticeQuestions",
                TestCaseID        = "GetPracticeQuestions_06",
                Description       = "Repository throws exception → propagates",
                ExpectedResult    = "Exception with 'DB failure'",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetRandomQuestionsForPracticeAsync throws" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetPracticeQuestions_07 | N | Question with PassageId fetches passage successfully
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_QuestionWithPassage_ShouldMapPassageContent()
        {
            var q1 = new QuestionBank
            {
                QuestionBankId = "QB-1", Content = "Q1", PassageId = "PASS-1",
                MediaUrl = null, QuestionOptions = new List<QuestionOption>()
            };

            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetRandomQuestionsForPracticeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<QuestionBank> { q1 });

            var psgRepo = new Mock<IPassageRepository>();
            psgRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new List<Passage> { new Passage { PassageId = "PASS-1", Content = "My Passage", ImageUrl = "pass.jpg" } });

            var handler = CreateHandler(qbRepo, psgRepo);
            var result = await handler.Handle(new GetPracticeQuestionsQuery { QuestionTypeId = "QT-1", Quantity = 1 }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data![0].SharedPassageContent.Should().Be("My Passage");
            result.Data![0].SharedMediaType.Should().Be("Image"); // Maps passage media if question has no media

            QACollector.LogTestCase("UserExam - Get Practice Questions", new TestCaseDetail
            {
                FunctionGroup     = "GetPracticeQuestions",
                TestCaseID        = "GetPracticeQuestions_07",
                Description       = "Question has PassageId, populates passageMap via GetByIdsAsync",
                ExpectedResult    = "Passage content correctly assigned to shared group",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "passageIds.Any() -> passageMap lookup" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // GetPracticeQuestions_08 | N | GetMediaType Unknown branch
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UnknownMediaExtension_ShouldReturnUnknownType()
        {
            var q1 = new QuestionBank
            {
                QuestionBankId = "QB-1", Content = "Q1", PassageId = null,
                MediaUrl = "test.txt", // unknown extension
                QuestionOptions = new List<QuestionOption>()
            };

            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetRandomQuestionsForPracticeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<QuestionBank> { q1 });

            var psgRepo = new Mock<IPassageRepository>();

            var handler = CreateHandler(qbRepo, psgRepo);
            var result = await handler.Handle(new GetPracticeQuestionsQuery { QuestionTypeId = "QT-1", Quantity = 1 }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data![0].SharedMediaType.Should().Be("Unknown");

            QACollector.LogTestCase("UserExam - Get Practice Questions", new TestCaseDetail
            {
                FunctionGroup     = "GetPracticeQuestions",
                TestCaseID        = "GetPracticeQuestions_08",
                Description       = "MediaUrl with unknown extension forces Unknown mapping",
                ExpectedResult    = "SharedMediaType=Unknown",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ext not in audio/image map" }
            });
        }
    }
}
