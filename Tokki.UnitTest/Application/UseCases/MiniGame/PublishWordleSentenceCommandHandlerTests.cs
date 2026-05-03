using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.Commands.PublishWordleSentence;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.MiniGame
{
    public class PublishWordleSentenceCommandHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static PublishWordleSentenceCommandHandler CreateHandler(
            Mock<IMiniGameRepository>? repo = null)
        {
            return new PublishWordleSentenceCommandHandler(
                (repo ?? new Mock<IMiniGameRepository>()).Object);
        }

        private static WordleSentenceSubmission BuildSubmission(string userId = "USER-001") => new()
        {
            SubmissionId = "SUB-001",
            UserId       = userId,
            IsPublic     = false,
            IsAnonymous  = false
        };

        // ═══════════════════════════════════════════════════════════════════
        // PublishWordleSentence_01 | 404 | Submission not found → Failure
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SubmissionNotFound_ShouldReturn404()
        {
            // Arrange
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((WordleSentenceSubmission?)null);

            var cmd = new PublishWordleSentenceCommand { SubmissionId = "INVALID", UserId = "USER-001", IsPublic = true };

            // Act
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Contain("Không tìm thấy");

            QACollector.LogTestCase("MiniGame - Publish Wordle", new TestCaseDetail
            {
                FunctionGroup     = "PublishWordleSentence",
                TestCaseID        = "PublishWordleSentence_01",
                Description       = "SubmissionId does not exist → 404 Failure",
                ExpectedResult    = "Return 404, message contains 'Không tìm thấy'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "submission == null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // PublishWordleSentence_02 | 403 | User is not the owner → Failure
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserNotOwner_ShouldReturn403()
        {
            // Arrange
            var submission = BuildSubmission(userId: "OWNER-001"); // owned by someone else

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(submission);

            var cmd = new PublishWordleSentenceCommand { SubmissionId = "SUB-001", UserId = "ATTACKER-002", IsPublic = true };

            // Act
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Message.Should().Contain("không có quyền");

            QACollector.LogTestCase("MiniGame - Publish Wordle", new TestCaseDetail
            {
                FunctionGroup     = "PublishWordleSentence",
                TestCaseID        = "PublishWordleSentence_02",
                Description       = "UserId does not match submission.UserId → 403 Forbidden",
                ExpectedResult    = "Return 403, message contains 'không có quyền'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "submission.UserId != request.UserId" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // PublishWordleSentence_03 | 200 | Valid owner makes public → IsPublic = true
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidOwner_MakePublic_ShouldSetIsPublicTrue()
        {
            // Arrange
            var submission = BuildSubmission("USER-001");

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(submission);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var cmd = new PublishWordleSentenceCommand { SubmissionId = "SUB-001", UserId = "USER-001", IsPublic = true, IsAnonymous = false };

            // Act
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            submission.IsPublic.Should().BeTrue();
            submission.IsAnonymous.Should().BeFalse();
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("MiniGame - Publish Wordle", new TestCaseDetail
            {
                FunctionGroup     = "PublishWordleSentence",
                TestCaseID        = "PublishWordleSentence_03",
                Description       = "Owner requests IsPublic=true → submission updated and saved",
                ExpectedResult    = "Return 200, submission.IsPublic = true, SaveChanges called",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "submission.IsPublic = request.IsPublic" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // PublishWordleSentence_04 | 200 | Owner sets IsAnonymous = true
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidOwner_SetAnonymous_ShouldSetIsAnonymousTrue()
        {
            // Arrange
            var submission = BuildSubmission("USER-001");

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(submission);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var cmd = new PublishWordleSentenceCommand { SubmissionId = "SUB-001", UserId = "USER-001", IsPublic = true, IsAnonymous = true };

            // Act
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            submission.IsAnonymous.Should().BeTrue();

            QACollector.LogTestCase("MiniGame - Publish Wordle", new TestCaseDetail
            {
                FunctionGroup     = "PublishWordleSentence",
                TestCaseID        = "PublishWordleSentence_04",
                Description       = "Owner sets IsAnonymous=true → flag persisted",
                ExpectedResult    = "Return 200, submission.IsAnonymous = true",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "submission.IsAnonymous = request.IsAnonymous" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // PublishWordleSentence_05 | 200 | Owner unpublishes (IsPublic = false)
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidOwner_Unpublish_ShouldSetIsPublicFalse()
        {
            // Arrange
            var submission = new WordleSentenceSubmission { SubmissionId = "SUB-001", UserId = "USER-001", IsPublic = true };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(submission);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var cmd = new PublishWordleSentenceCommand { SubmissionId = "SUB-001", UserId = "USER-001", IsPublic = false };

            // Act
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            submission.IsPublic.Should().BeFalse();

            QACollector.LogTestCase("MiniGame - Publish Wordle", new TestCaseDetail
            {
                FunctionGroup     = "PublishWordleSentence",
                TestCaseID        = "PublishWordleSentence_05",
                Description       = "Owner unpublishes previously public submission → IsPublic = false",
                ExpectedResult    = "Return 200, submission.IsPublic = false",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsPublic = false (toggle off)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // PublishWordleSentence_06 | 500 | SaveChanges throws → exception propagates
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SaveChangesThrows_ShouldPropagateException()
        {
            // Arrange
            var submission = BuildSubmission("USER-001");

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(submission);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Concurrency conflict"));

            var cmd = new PublishWordleSentenceCommand { SubmissionId = "SUB-001", UserId = "USER-001", IsPublic = true };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo).Handle(cmd, CancellationToken.None));

            QACollector.LogTestCase("MiniGame - Publish Wordle", new TestCaseDetail
            {
                FunctionGroup     = "PublishWordleSentence",
                TestCaseID        = "PublishWordleSentence_06",
                Description       = "SaveChangesAsync throws → exception propagates unhandled",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SaveChangesAsync throws" }
            });
        }
    }
}
