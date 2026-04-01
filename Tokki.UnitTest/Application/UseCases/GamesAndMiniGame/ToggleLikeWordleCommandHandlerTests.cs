using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.Commands.ToggleLikeWordle;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.GamesAndMiniGame
{
    public class ToggleLikeWordleCommandHandlerTests
    {
        private ToggleLikeWordleCommandHandler CreateHandler(
            Mock<IMiniGameRepository>? repo = null)
        {
            return new ToggleLikeWordleCommandHandler(
                (repo ?? new Mock<IMiniGameRepository>()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_SubmissionNotFound_ShouldReturn404()
        {
            var command = new ToggleLikeWordleCommand
            {
                SubmissionId = "SUB-INVALID",
                UserId       = "USER-001"
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((WordleSentenceSubmission?)null);

            var result = await CreateHandler(repo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Wordle - Toggle Like", new TestCaseDetail
            {
                FunctionGroup     = "Toggle Like Wordle",
                TestCaseID        = "TC-WDL-LKE-01",
                Description       = "Like/Unlike with SubmissionId does not exist",
                ExpectedResult    = "Return 404 Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid SubmissionId", "Return 404" }
            });
        }

        [Fact]
        public async Task Handle_LikeExists_ShouldUnlikeAndDecrementCount()
        {
            var command = new ToggleLikeWordleCommand { SubmissionId = "SUB-001", UserId = "USER-001" };

            var submission = new WordleSentenceSubmission { SubmissionId = "SUB-001", LikeCount = 5 };
            var existingLike = new WordleSentenceLike { LikeId = "LIKE-001", SubmissionId = "SUB-001", UserId = "USER-001" };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync("SUB-001", It.IsAny<CancellationToken>())).ReturnsAsync(submission);
            mockRepo.Setup(x => x.GetLikeAsync("USER-001", "SUB-001", It.IsAny<CancellationToken>())).ReturnsAsync(existingLike);
            mockRepo.Setup(x => x.RemoveLike(It.IsAny<WordleSentenceLike>()));
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await CreateHandler(repo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            submission.LikeCount.Should().Be(4);
            mockRepo.Verify(x => x.RemoveLike(It.IsAny<WordleSentenceLike>()), Times.Once);

            QACollector.LogTestCase("Wordle - Toggle Like", new TestCaseDetail
            {
                FunctionGroup     = "Toggle Like Wordle",
                TestCaseID        = "TC-WDL-LKE-02",
                Description       = "User liked → unlike, LikeCount decreased by 1",
                ExpectedResult    = "Return Success, LikeCount = 4, RemoveLike called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Existing Like (toggle → unlike)", "LikeCount = 5 → 4" }
            });
        }

        [Fact]
        public async Task Handle_NoExistingLike_ShouldAddLikeAndIncrementCount()
        {
            var command = new ToggleLikeWordleCommand { SubmissionId = "SUB-001", UserId = "USER-002" };
            var submission = new WordleSentenceSubmission { SubmissionId = "SUB-001", LikeCount = 3 };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(submission);
            mockRepo.Setup(x => x.GetLikeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((WordleSentenceLike?)null);
            mockRepo.Setup(x => x.AddLike(It.IsAny<WordleSentenceLike>()));
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await CreateHandler(repo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            submission.LikeCount.Should().Be(4);
            mockRepo.Verify(x => x.AddLike(It.IsAny<WordleSentenceLike>()), Times.Once);

            QACollector.LogTestCase("Wordle - Toggle Like", new TestCaseDetail
            {
                FunctionGroup     = "Toggle Like Wordle",
                TestCaseID        = "TC-WDL-LKE-03",
                Description       = "User has not liked yet → add like, LikeCount increases by 1",
                ExpectedResult    = "Return Success, LikeCount = 4, AddLike called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No existing Like (toggle → like)", "LikeCount = 3 → 4" }
            });
        }

        [Fact]
        public async Task Handle_LikeCountAtZero_Unliking_ShouldNotGoNegative()
        {
            var command = new ToggleLikeWordleCommand { SubmissionId = "SUB-001", UserId = "USER-001" };
            var submission = new WordleSentenceSubmission { SubmissionId = "SUB-001", LikeCount = 0 };
            var existingLike = new WordleSentenceLike { LikeId = "LIKE-001", SubmissionId = "SUB-001", UserId = "USER-001" };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(submission);
            mockRepo.Setup(x => x.GetLikeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(existingLike);
            mockRepo.Setup(x => x.RemoveLike(It.IsAny<WordleSentenceLike>()));
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await CreateHandler(repo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            submission.LikeCount.Should().BeGreaterThanOrEqualTo(0);

            QACollector.LogTestCase("Wordle - Toggle Like", new TestCaseDetail
            {
                FunctionGroup     = "Toggle Like Wordle",
                TestCaseID        = "TC-WDL-LKE-04",
                Description       = "LikeCount = 0, user unlikes → count does not go negative",
                ExpectedResult    = "Return Success, LikeCount >= 0",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "LikeCount=0 (boundary minimum)", "RemoveLike called" }
            });
        }

        [Fact]
        public async Task Handle_MultipleUsersLike_SameSubmission_EachIncrements()
        {
            var submission = new WordleSentenceSubmission { SubmissionId = "SUB-001", LikeCount = 10 };

            var mockRepo1 = new Mock<IMiniGameRepository>();
            mockRepo1.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(submission);
            mockRepo1.Setup(x => x.GetLikeAsync("USER-002", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((WordleSentenceLike?)null);
            mockRepo1.Setup(x => x.AddLike(It.IsAny<WordleSentenceLike>()));
            mockRepo1.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var cmd2 = new ToggleLikeWordleCommand { SubmissionId = "SUB-001", UserId = "USER-002" };
            var result = await CreateHandler(repo: mockRepo1).Handle(cmd2, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            submission.LikeCount.Should().Be(11);

            QACollector.LogTestCase("Wordle - Toggle Like", new TestCaseDetail
            {
                FunctionGroup     = "Toggle Like Wordle",
                TestCaseID        = "TC-WDL-LKE-05",
                Description       = "A new user (USER-002) likes a submission with LikeCount=10 → count=11",
                ExpectedResult    = "Return Success, LikeCount = 11",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Different user, new like → LikeCount increments" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB failure"));

            var command = new ToggleLikeWordleCommand { SubmissionId = "SUB-001", UserId = "USER-001" };

            await Assert.ThrowsAsync<Exception>(() => CreateHandler(repo: mockRepo).Handle(command, CancellationToken.None));

            QACollector.LogTestCase("Wordle - Toggle Like", new TestCaseDetail
            {
                FunctionGroup     = "Toggle Like Wordle",
                TestCaseID        = "TC-WDL-LKE-06",
                Description       = "Repository throws DB failure → exception propagates",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetWordleSubmissionByIdAsync throws" }
            });
        }
    }
}
