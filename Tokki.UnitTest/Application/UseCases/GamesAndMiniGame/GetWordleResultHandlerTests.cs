using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.Queries.Wordle;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.GamesAndMiniGame
{
    public class GetWordleResultHandlerTests
    {
        private static GetWordleResultHandler CreateHandler(
            Mock<IMiniGameRepository>? miniGameRepo = null,
            Mock<IVocabularyRepository>? vocabRepo = null)
        {
            return new GetWordleResultHandler(
                (miniGameRepo ?? new Mock<IMiniGameRepository>()).Object,
                (vocabRepo ?? new Mock<IVocabularyRepository>()).Object);
        }

        private static GetWordleResultQuery ValidQuery => new()
        {
            UserId        = "USER-001",
            DailyWordleId = "DW-001"
        };

        private static DailyWordle BuildGame(string wordleId = "DW-001", string word = "가나다") => new()
        {
            DailyWordleId = wordleId,
            Word          = word,
            VocabularyId  = "VOCAB-001"
        };

        private static Tokki.Domain.Entities.Vocabulary BuildVocab() => new()
        {
            VocabularyId = "VOCAB-001",
            Definition   = "Korean greeting",
            ImgURL       = "hello.png",
            AudioURL     = "hello.mp3"
        };

        [Fact]
        public async Task Handle_NoProgress_ShouldReturnFailure()
        {
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetUserWordleProgressAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress>());

            var result = await CreateHandler(mockRepo).Handle(ValidQuery, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("hoàn thành");

            QACollector.LogTestCase("MiniGame - Get Wordle Result", new TestCaseDetail
            {
                FunctionGroup     = "GetWordleResult",
                TestCaseID        = "TC-WDL-RST-01",
                Description       = "User has no progress → Failure with must-win message",
                ExpectedResult    = "Return Failure, message contains 'hoàn thành'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "progress == null" }
            });
        }

        [Fact]
        public async Task Handle_ProgressExistsButNotWon_ShouldReturnFailure()
        {
            var progress = new UserWordleProgress { DailyWordleId = "DW-001", IsWon = false, AttemptCount = 3 };
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetUserWordleProgressAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress> { progress });

            var result = await CreateHandler(mockRepo).Handle(ValidQuery, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("chiến thắng");

            QACollector.LogTestCase("MiniGame - Get Wordle Result", new TestCaseDetail
            {
                FunctionGroup     = "GetWordleResult",
                TestCaseID        = "TC-WDL-RST-02",
                Description       = "Progress exists but IsWon=false → Failure",
                ExpectedResult    = "Return Failure, message contains 'chiến thắng'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!progress.IsWon => Failure" }
            });
        }

        [Fact]
        public async Task Handle_WonButGameNotFound_ShouldReturnFailure()
        {
            var progress = new UserWordleProgress { DailyWordleId = "DW-001", IsWon = true, AttemptCount = 2 };
            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetUserWordleProgressAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress> { progress });
            mockRepo.Setup(x => x.GetDailyWordleByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((DailyWordle?)null);

            var result = await CreateHandler(mockRepo).Handle(ValidQuery, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy");

            QACollector.LogTestCase("MiniGame - Get Wordle Result", new TestCaseDetail
            {
                FunctionGroup     = "GetWordleResult",
                TestCaseID        = "TC-WDL-RST-03",
                Description       = "User won but game deleted → Failure 'Không tìm thấy'",
                ExpectedResult    = "Return Failure, message 'Không tìm thấy'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "game == null" }
            });
        }

        [Fact]
        public async Task Handle_WonAndGameFound_ShouldReturnWordleResultDto()
        {
            var progress = new UserWordleProgress { DailyWordleId = "DW-001", IsWon = true, AttemptCount = 4 };
            var game     = BuildGame();
            var vocab    = BuildVocab();

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetUserWordleProgressAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress> { progress });
            mockRepo.Setup(x => x.GetDailyWordleByIdAsync("DW-001", It.IsAny<CancellationToken>())).ReturnsAsync(game);

            var mockVocab = new Mock<IVocabularyRepository>();
            mockVocab.Setup(x => x.GetByIdAsync("VOCAB-001")).ReturnsAsync(vocab);

            var result = await CreateHandler(miniGameRepo: mockRepo, vocabRepo: mockVocab).Handle(ValidQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Word.Should().Be("가나다");
            result.Data.Definition.Should().Be("Korean greeting");
            result.Data.AttemptCount.Should().Be(4);
            result.Data.ImageUrl.Should().Be("hello.png");
            result.Data.AudioUrl.Should().Be("hello.mp3");

            QACollector.LogTestCase("MiniGame - Get Wordle Result", new TestCaseDetail
            {
                FunctionGroup     = "GetWordleResult",
                TestCaseID        = "TC-WDL-RST-04",
                Description       = "Win, game and vocab found → full WordleResultDTO returned",
                ExpectedResult    = "Return 200, Word/Definition/AttemptCount/ImageUrl/AudioUrl correct",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "progress.IsWon = true, game != null, vocab != null" }
            });
        }

        [Fact]
        public async Task Handle_WonAndGameFound_ShouldMapDailyWordleId()
        {
            var progress = new UserWordleProgress { DailyWordleId = "DW-001", IsWon = true, AttemptCount = 1 };
            var game     = BuildGame("DW-001", "안녕해");
            var vocab    = BuildVocab();

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetUserWordleProgressAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress> { progress });
            mockRepo.Setup(x => x.GetDailyWordleByIdAsync("DW-001", It.IsAny<CancellationToken>())).ReturnsAsync(game);

            var mockVocab = new Mock<IVocabularyRepository>();
            mockVocab.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(vocab);

            var result = await CreateHandler(miniGameRepo: mockRepo, vocabRepo: mockVocab).Handle(ValidQuery, CancellationToken.None);

            result.Data!.DailyWordleId.Should().Be("DW-001");

            QACollector.LogTestCase("MiniGame - Get Wordle Result", new TestCaseDetail
            {
                FunctionGroup     = "GetWordleResult",
                TestCaseID        = "TC-WDL-RST-05",
                Description       = "DailyWordleId from game mapped to result DTO",
                ExpectedResult    = "DTO.DailyWordleId = 'DW-001'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "result.DailyWordleId = game.DailyWordleId" }
            });
        }

        [Fact]
        public async Task Handle_VocabRepositoryThrows_ShouldPropagateException()
        {
            var progress = new UserWordleProgress { DailyWordleId = "DW-001", IsWon = true, AttemptCount = 2 };
            var game     = BuildGame();

            var mockRepo = new Mock<IMiniGameRepository>();
            mockRepo.Setup(x => x.GetUserWordleProgressAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<UserWordleProgress> { progress });
            mockRepo.Setup(x => x.GetDailyWordleByIdAsync("DW-001", It.IsAny<CancellationToken>())).ReturnsAsync(game);

            var mockVocab = new Mock<IVocabularyRepository>();
            mockVocab.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Vocab DB error"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(miniGameRepo: mockRepo, vocabRepo: mockVocab).Handle(ValidQuery, CancellationToken.None));

            QACollector.LogTestCase("MiniGame - Get Wordle Result", new TestCaseDetail
            {
                FunctionGroup     = "GetWordleResult",
                TestCaseID        = "TC-WDL-RST-06",
                Description       = "VocabularyRepository throws → exception propagates unhandled",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "vocabRepo.GetByIdAsync throws" }
            });
        }
    }
}
