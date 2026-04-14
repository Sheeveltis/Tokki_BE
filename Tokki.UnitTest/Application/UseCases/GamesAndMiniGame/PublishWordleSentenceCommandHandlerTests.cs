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

namespace Tokki.UnitTest.Application.UseCases.GamesAndMiniGame
{
    public class PublishWordleSentenceCommandHandlerTests
    {
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

        [Fact]
        public async Task Handle_SubmissionNotFound_ShouldReturn404()
        {
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((WordleSentenceSubmission?)null);

            var cmd = new PublishWordleSentenceCommand { SubmissionId = "INVALID", UserId = "USER-001", IsPublic = true };
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Contain("Không tìm thấy");

            QACollector.LogTestCase("MiniGame - Publish Wordle", new TestCaseDetail
            {
                FunctionGroup     = "PublishWordleSentence",
                TestCaseID        = "TC-WDL-PUB-01",
                Description       = "SubmissionId does not exist → 404 Failure",
                ExpectedResult    = "Return 404, message contains 'Không tìm thấy'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "submission == null" }
            });
        }

        [Fact]
        public async Task Handle_UserNotOwner_ShouldReturn403()
        {
            var submission = BuildSubmission(userId: "OWNER-001");
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(submission);

            var cmd = new PublishWordleSentenceCommand { SubmissionId = "SUB-001", UserId = "ATTACKER-002", IsPublic = true };
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Message.Should().Contain("không có quyền");

            QACollector.LogTestCase("MiniGame - Publish Wordle", new TestCaseDetail
            {
                FunctionGroup     = "PublishWordleSentence",
                TestCaseID        = "TC-WDL-PUB-02",
                Description       = "UserId does not match submission.UserId → 403 Forbidden",
                ExpectedResult    = "Return 403, message contains 'không có quyền'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "submission.UserId != request.UserId" }
            });
        }

        [Fact]
        public async Task Handle_ValidOwner_MakePublic_ShouldSetIsPublicTrue()
        {
            var submission = BuildSubmission("USER-001");
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(submission);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var cmd = new PublishWordleSentenceCommand { SubmissionId = "SUB-001", UserId = "USER-001", IsPublic = true, IsAnonymous = false };
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            submission.IsPublic.Should().BeTrue();
            submission.IsAnonymous.Should().BeFalse();
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("MiniGame - Publish Wordle", new TestCaseDetail
            {
                FunctionGroup     = "PublishWordleSentence",
                TestCaseID        = "TC-WDL-PUB-03",
                Description       = "Owner requests IsPublic=true → submission updated and saved",
                ExpectedResult    = "Return 200, submission.IsPublic = true, SaveChanges called",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "submission.IsPublic = request.IsPublic" }
            });
        }

        [Fact]
        public async Task Handle_ValidOwner_SetAnonymous_ShouldSetIsAnonymousTrue()
        {
            var submission = BuildSubmission("USER-001");
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(submission);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var cmd = new PublishWordleSentenceCommand { SubmissionId = "SUB-001", UserId = "USER-001", IsPublic = true, IsAnonymous = true };
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            submission.IsAnonymous.Should().BeTrue();

            QACollector.LogTestCase("MiniGame - Publish Wordle", new TestCaseDetail
            {
                FunctionGroup     = "PublishWordleSentence",
                TestCaseID        = "TC-WDL-PUB-04",
                Description       = "Owner sets IsAnonymous=true → flag persisted",
                ExpectedResult    = "Return 200, submission.IsAnonymous = true",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "submission.IsAnonymous = request.IsAnonymous" }
            });
        }

        [Fact]
        public async Task Handle_ValidOwner_Unpublish_ShouldSetIsPublicFalse()
        {
            var submission = new WordleSentenceSubmission { SubmissionId = "SUB-001", UserId = "USER-001", IsPublic = true };
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(submission);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var cmd = new PublishWordleSentenceCommand { SubmissionId = "SUB-001", UserId = "USER-001", IsPublic = false };
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            submission.IsPublic.Should().BeFalse();

            QACollector.LogTestCase("MiniGame - Publish Wordle", new TestCaseDetail
            {
                FunctionGroup     = "PublishWordleSentence",
                TestCaseID        = "TC-WDL-PUB-05",
                Description       = "Owner unpublishes previously public submission → IsPublic = false",
                ExpectedResult    = "Return 200, submission.IsPublic = false",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsPublic = false (toggle off)" }
            });
        }

        [Fact]
        public async Task Handle_SaveChangesThrows_ShouldPropagateException()
        {
            var submission = BuildSubmission("USER-001");
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(submission);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Concurrency conflict"));

            var cmd = new PublishWordleSentenceCommand { SubmissionId = "SUB-001", UserId = "USER-001", IsPublic = true };

            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo).Handle(cmd, CancellationToken.None));

            QACollector.LogTestCase("MiniGame - Publish Wordle", new TestCaseDetail
            {
                FunctionGroup     = "PublishWordleSentence",
                TestCaseID        = "TC-WDL-PUB-06",
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
