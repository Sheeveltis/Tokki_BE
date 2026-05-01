using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.Commands.SubmitWordleGuess;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.MiniGame
{
    public class SubmitWordleGuessHandlerTests
    {
        private SubmitWordleGuessHandler CreateHandler(
            Mock<IMiniGameRepository>? repo = null)
        {
            return new SubmitWordleGuessHandler(
                (repo ?? new Mock<IMiniGameRepository>()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_DailyWordleNotFound_ShouldReturnFailure()
        {
            var command = new SubmitWordleGuessCommand
            {
                DailyWordleId = "WORDLE-INVALID",
                GuessWord = "안녕",
                UserId = "USER-001"
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordleByIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Domain.Entities.DailyWordle?)null);

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Wordle - Submit Guess", new TestCaseDetail
            {
                FunctionGroup = "Submit Wordle Guess",
                TestCaseID = "Submit_Wordle_Guess_01",
                Description = "Submit guess with DailyWordleId does not exist",
                ExpectedResult = "Return Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid DailyWordleId",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_WrongGuessLength_ShouldReturnFailure()
        {
            var command = new SubmitWordleGuessCommand
            {
                DailyWordleId = "WORDLE-001",
                GuessWord = "짧",    // 1 ký tự trong khi target là 3 ký tự
                UserId = "USER-001"
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordleByIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DailyWordle { Word = "안녕해" }); // 3 chars

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Wordle - Submit Guess", new TestCaseDetail
            {
                FunctionGroup = "Submit Wordle Guess",
                TestCaseID = "Submit_Wordle_Guess_02",
                Description = "GuessWord's length is wrong compared to the target",
                ExpectedResult = "Return Failure with invalid message length",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "GuessWord.Length != TargetWord.Length",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_GameAlreadyOver_ShouldReturnFailure()
        {
            // User đã thắng rồi → không cho đoán nữa
            var command = new SubmitWordleGuessCommand
            {
                DailyWordleId = "WORDLE-001",
                GuessWord = "안녕해",
                UserId = "USER-001"
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordleByIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DailyWordle { Word = "안녕해" });

            // Progress đã IsWon = true
            mockRepo.Setup(x => x.GetUserWordleProgressAsync(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress>
                    {
                        new UserWordleProgress
                        {
                            IsWon = true,
                            AttemptCount = 3,
                            Guesses = new List<string> { "가나다", "라마바", "안녕해" }
                        }
                    });

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("end");

            QACollector.LogTestCase("Wordle - Submit Guess", new TestCaseDetail
            {
                FunctionGroup = "Submit Wordle Guess",
                TestCaseID = "Submit_Wordle_Guess_03",
                Description = "User has won (IsWon = true) → no more guesses are allowed",
                ExpectedResult = "Return Failure 'turn has ended'",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "IsWon = true (boundary: game over)",
                    "Return Failure"
                }
            });
        }
    }
}