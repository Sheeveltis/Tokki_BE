using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.Queries.CheckGradingStatus;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class CheckGradingStatusQueryHandlerTests
    {
        private static CheckGradingStatusQueryHandler CreateHandler(Mock<IUserExamRepository>? repo = null)
            => new CheckGradingStatusQueryHandler((repo ?? new Mock<IUserExamRepository>()).Object);

        private static CheckGradingStatusQuery MakeQuery(string userExamId = "UE-001")
            => new CheckGradingStatusQuery { UserExamId = userExamId };

        // ═══════════════════════════════════════════════════════════════
        // CheckGradingStatus_01 | N | All writing graded → IsGraded=true
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoPendingWriting_ShouldReturnIsGradedTrue()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.HasPendingWritingAnswersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.IsGraded.Should().BeTrue();

            // Excel Log
            QACollector.LogTestCase("UserExam - Check Grading", new TestCaseDetail
            {
                FunctionGroup     = "CheckGradingStatus",
                TestCaseID        = "CheckGradingStatus_01",
                Description       = "No pending writing answers → IsGraded=true",
                ExpectedResult    = "IsSuccess=true, IsGraded=true",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "HasPendingWritingAnswersAsync=false" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // CheckGradingStatus_02 | N | Writing still pending → IsGraded=false
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_HasPendingWriting_ShouldReturnIsGradedFalse()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.HasPendingWritingAnswersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.IsGraded.Should().BeFalse();

            // Excel Log
            QACollector.LogTestCase("UserExam - Check Grading", new TestCaseDetail
            {
                FunctionGroup     = "CheckGradingStatus",
                TestCaseID        = "CheckGradingStatus_02",
                Description       = "Writing answers still pending → IsGraded=false",
                ExpectedResult    = "IsSuccess=true, IsGraded=false",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "HasPendingWritingAnswersAsync=true" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // CheckGradingStatus_03 | N | Repository called with correct UserExamId
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ShouldCallRepoWithCorrectId()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.HasPendingWritingAnswersAsync("UE-999", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var handler = CreateHandler(repo);

            // Act
            await handler.Handle(MakeQuery("UE-999"), CancellationToken.None);

            // Assert
            repo.Verify(x => x.HasPendingWritingAnswersAsync("UE-999", It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("UserExam - Check Grading", new TestCaseDetail
            {
                FunctionGroup     = "CheckGradingStatus",
                TestCaseID        = "CheckGradingStatus_03",
                Description       = "Repository called with the correct UserExamId",
                ExpectedResult    = "HasPendingWritingAnswersAsync called with 'UE-999'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserExamId forwarded correctly" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // CheckGradingStatus_04 | N | Always returns 200 (no 404/400 logic)
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AnyInput_ShouldAlwaysReturnSuccess()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.HasPendingWritingAnswersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery("WHATEVER"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // Excel Log
            QACollector.LogTestCase("UserExam - Check Grading", new TestCaseDetail
            {
                FunctionGroup     = "CheckGradingStatus",
                TestCaseID        = "CheckGradingStatus_04",
                Description       = "Handler has no guard clauses → always returns Success",
                ExpectedResult    = "IsSuccess=true regardless of input",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No 404/400 logic in this handler" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // CheckGradingStatus_05 | B | Empty UserExamId passed → still calls repo
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyUserExamId_ShouldStillCallRepo()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.HasPendingWritingAnswersAsync(string.Empty, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(MakeQuery(""), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // Excel Log
            QACollector.LogTestCase("UserExam - Check Grading", new TestCaseDetail
            {
                FunctionGroup     = "CheckGradingStatus",
                TestCaseID        = "CheckGradingStatus_05",
                Description       = "Empty UserExamId → handler still delegates to repo (no validation at handler level)",
                ExpectedResult    = "IsSuccess=true",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserExamId = empty string" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // CheckGradingStatus_06 | E | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.HasPendingWritingAnswersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB error"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(MakeQuery(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");

            // Excel Log
            QACollector.LogTestCase("UserExam - Check Grading", new TestCaseDetail
            {
                FunctionGroup     = "CheckGradingStatus",
                TestCaseID        = "CheckGradingStatus_06",
                Description       = "Repository throws exception → propagates",
                ExpectedResult    = "Exception thrown with 'DB error'",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "HasPendingWritingAnswersAsync throws Exception" }
            });
        }
    }
}
