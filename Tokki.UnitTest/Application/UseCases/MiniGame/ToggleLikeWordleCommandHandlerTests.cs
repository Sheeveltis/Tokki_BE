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

namespace Tokki.UnitTest.Application.UseCases.MiniGame
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
                UserId = "USER-001"
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((WordleSentenceSubmission?)null);

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Wordle - Toggle Like", new TestCaseDetail
            {
                FunctionGroup = "Toggle Like Wordle",
                TestCaseID = "Toggle_Like_Wordle_01",
                Description = "Like/Unlike with SubmissionId does not exist",
                ExpectedResult = "Return 404 Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid SubmissionId",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_LikeExists_ShouldUnlikeAndDecrementCount()
        {
            var command = new ToggleLikeWordleCommand
            {
                SubmissionId = "SUB-001",
                UserId = "USER-001"
            };

            var submission = new WordleSentenceSubmission
            {
                SubmissionId = "SUB-001",
                LikeCount = 5
            };

            var existingLike = new WordleSentenceLike
            {
                LikeId = "LIKE-001",
                SubmissionId = "SUB-001",
                UserId = "USER-001"
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(
                        "SUB-001",
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(submission);

            mockRepo.Setup(x => x.GetLikeAsync(
                        "USER-001",
                        "SUB-001",
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingLike); // đã like rồi → unlike

            mockRepo.Setup(x => x.RemoveLike(It.IsAny<WordleSentenceLike>()));
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
         .ReturnsAsync(1);

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            submission.LikeCount.Should().Be(4); // 5 - 1

            mockRepo.Verify(x => x.RemoveLike(It.IsAny<WordleSentenceLike>()), Times.Once);

            QACollector.LogTestCase("Wordle - Toggle Like", new TestCaseDetail
            {
                FunctionGroup = "Toggle Like Wordle",
                TestCaseID = "Toggle_Like_Wordle_02",
                Description = "User liked → unlike, LikeCount decreased by 1",
                ExpectedResult = "Return Success, LikeCount = 4, RemoveLike called once",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Existing Like (toggle → unlike)",
                    "LikeCount = 5 → 4",
                    "RemoveLike called once"
                }
            });
        }

        [Fact]
        public async Task Handle_NoExistingLike_ShouldAddLikeAndIncrementCount()
        {
            var command = new ToggleLikeWordleCommand
            {
                SubmissionId = "SUB-001",
                UserId = "USER-002"
            };

            var submission = new WordleSentenceSubmission
            {
                SubmissionId = "SUB-001",
                LikeCount = 3
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetWordleSubmissionByIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(submission);

            mockRepo.Setup(x => x.GetLikeAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((WordleSentenceLike?)null); // chưa like

            mockRepo.Setup(x => x.AddLike(It.IsAny<WordleSentenceLike>()));
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            submission.LikeCount.Should().Be(4); // 3 + 1

            mockRepo.Verify(x => x.AddLike(It.IsAny<WordleSentenceLike>()), Times.Once);

            QACollector.LogTestCase("Wordle - Toggle Like", new TestCaseDetail
            {
                FunctionGroup = "Toggle Like Wordle",
                TestCaseID = "Toggle_Like_Wordle_03",
                Description = "User has not liked yet → add like, LikeCount increases by 1",
                ExpectedResult = "Return Success, LikeCount = 4, AddLike called once",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "No existing Like (toggle → like)",
                    "LikeCount = 3 → 4",
                    "AddLike called once"
                }
            });
        }
    }
}