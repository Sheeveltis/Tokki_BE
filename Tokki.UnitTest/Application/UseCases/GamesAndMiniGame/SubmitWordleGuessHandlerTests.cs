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

namespace Tokki.UnitTest.Application.UseCases.GamesAndMiniGame
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
                GuessWord     = "안녕",
                UserId        = "USER-001"
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordleByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((DailyWordle?)null);

            var result = await CreateHandler(repo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Wordle - Submit Guess", new TestCaseDetail
            {
                FunctionGroup     = "Submit Wordle Guess",
                TestCaseID        = "TC-WDL-GUS-01",
                Description       = "Submit guess with DailyWordleId does not exist",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid DailyWordleId", "Return Failure" }
            });
        }

        [Fact]
        public async Task Handle_WrongGuessLength_ShouldReturnFailure()
        {
            var command = new SubmitWordleGuessCommand
            {
                DailyWordleId = "WORDLE-001",
                GuessWord     = "짧",   // 1 char but target is 3
                UserId        = "USER-001"
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordleByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DailyWordle { Word = "안녕해" }); // 3 chars

            var result = await CreateHandler(repo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Wordle - Submit Guess", new TestCaseDetail
            {
                FunctionGroup     = "Submit Wordle Guess",
                TestCaseID        = "TC-WDL-GUS-02",
                Description       = "GuessWord's length is wrong compared to the target",
                ExpectedResult    = "Return Failure with invalid message length",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GuessWord.Length != TargetWord.Length", "Return Failure" }
            });
        }

        [Fact]
        public async Task Handle_GameAlreadyOver_ShouldReturnFailure()
        {
            var command = new SubmitWordleGuessCommand
            {
                DailyWordleId = "WORDLE-001",
                GuessWord     = "안녕해",
                UserId        = "USER-001"
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordleByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DailyWordle { Word = "안녕해" });

            mockRepo.Setup(x => x.GetUserWordleProgressAsync(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress>
                    {
                        new UserWordleProgress { IsWon = true, AttemptCount = 3, Guesses = new List<string> { "가나다", "라마바", "안녕해" } }
                    });

            var result = await CreateHandler(repo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("end");

            QACollector.LogTestCase("Wordle - Submit Guess", new TestCaseDetail
            {
                FunctionGroup     = "Submit Wordle Guess",
                TestCaseID        = "TC-WDL-GUS-03",
                Description       = "User has won (IsWon = true) → no more guesses are allowed",
                ExpectedResult    = "Return Failure 'turn has ended'",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsWon = true (boundary: game over)", "Return Failure" }
            });
        }

        [Fact]
        public async Task Handle_CorrectGuess_ShouldReturnWinResult()
        {
            var command = new SubmitWordleGuessCommand
            {
                DailyWordleId = "WORDLE-001",
                GuessWord     = "가나다",
                UserId        = "USER-001"
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordleByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DailyWordle { DailyWordleId = "WORDLE-001", Word = "가나다" });

            mockRepo.Setup(x => x.GetUserWordleProgressAsync(
                        It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress>());

            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
         .ReturnsAsync(1);
            var result = await CreateHandler(repo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.IsWon.Should().BeTrue();


            QACollector.LogTestCase("Wordle - Submit Guess", new TestCaseDetail
            {
                FunctionGroup     = "Submit Wordle Guess",
                TestCaseID        = "TC-WDL-GUS-04",
                Description       = "GuessWord matches TargetWord exactly → IsWon = true",
                ExpectedResult    = "Return Success, IsWon = true",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GuessWord == TargetWord => IsWon = true" }
            });
        }

        [Fact]
        public async Task Handle_WrongGuess_CorrectCharAt0_ShouldReturnCorrectFeedback()
        {
            var command = new SubmitWordleGuessCommand
            {
                DailyWordleId = "WORDLE-001",
                GuessWord     = "가마바",  // 가 correct, 마바 wrong
                UserId        = "USER-001"
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordleByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DailyWordle { DailyWordleId = "WORDLE-001", Word = "가나다" });

            mockRepo.Setup(x => x.GetUserWordleProgressAsync(
                        It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress>());

            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(1);
            var result = await CreateHandler(repo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.IsWon.Should().BeFalse();
            result.Data.Feedbacks[0].InitialStatus.Should().Be("correct");   // 가 = correct
            result.Data.Feedbacks[1].InitialStatus.Should().Be("incorrect"); // 마 ≠ 나
            result.Data.Feedbacks[2].InitialStatus.Should().Be("incorrect"); // 바 ≠ 다

            QACollector.LogTestCase("Wordle - Submit Guess", new TestCaseDetail
            {
                FunctionGroup     = "Submit Wordle Guess",
                TestCaseID        = "TC-WDL-GUS-05",
                Description       = "Partial match: char[0]='가' correct, [1]='마' and [2]='바' incorrect",
                ExpectedResult    = "TileResults[0]='correct', [1]='incorrect', [2]='incorrect', IsWon=false",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GuessChar[0] matches TargetChar[0]", "GuessChar[1,2] do not match" }
            });
        }

        [Fact]
        public async Task Handle_MaxAttemptsReached_ShouldReturnGameOver()
        {
            var command = new SubmitWordleGuessCommand
            {
                DailyWordleId = "WORDLE-001",
                GuessWord     = "라마바",
                UserId        = "USER-001"
            };

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetDailyWordleByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DailyWordle { DailyWordleId = "WORDLE-001", Word = "가나다" });

            // 5 failed guesses already
            var existingProgress = new UserWordleProgress
            {
                IsWon        = false,
                AttemptCount = 5,
                Guesses      = new List<string> { "가마바", "나마바", "다마바", "라마바", "마바사" }
            };

            mockRepo.Setup(x => x.GetUserWordleProgressAsync(
                        It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress> { existingProgress });

            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1);
            var result = await CreateHandler(repo: mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.IsWon.Should().BeFalse();
            result.Data.IsGameOver.Should().BeTrue();

            QACollector.LogTestCase("Wordle - Submit Guess", new TestCaseDetail
            {
                FunctionGroup     = "Submit Wordle Guess",
                TestCaseID        = "TC-WDL-GUS-06",
                Description       = "6th attempt (MaxAttempts=6) reached without winning → IsGameOver = true",
                ExpectedResult    = "Return Success, IsWon=false, IsGameOver=true",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AttemptCount >= MaxAttempts (6) => IsGameOver = true" }
            });
        }
    }
}
