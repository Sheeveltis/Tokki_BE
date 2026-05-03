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

namespace Tokki.UnitTest.Application.UseCases.MiniGame.Queries.Wordle
{
    public class GetWordleResultHandlerTests
    {
        private readonly Mock<IMiniGameRepository> _miniGameMock = new();
        private readonly Mock<IVocabularyRepository> _vocabMock = new();

        private GetWordleResultHandler CreateHandler()
        {
            return new GetWordleResultHandler(_miniGameMock.Object, _vocabMock.Object);
        }

        // -----------------------------------------------------------
        // GetWordleResultHandler_01 | A | Progress Is Null -> Fail
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ProgressNull_ShouldReturnFailure()
        {
            _miniGameMock.Setup(x => x.GetUserWordleProgressAsync("u", new[] { "w1" }, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<UserWordleProgress>());
            var handler = CreateHandler();
            var result = await handler.Handle(new GetWordleResultQuery { UserId = "u", DailyWordleId = "w1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("B?n c?n hoàn thành");

            QACollector.LogTestCase("MiniGame - Get Wordle Result", new TestCaseDetail
            {
                FunctionGroup = "GetWordleResultHandler",
                TestCaseID = "GetWordleResultHandler_01",
                Description = "Checks missing bounds securely restricting unearned access natively",
                ExpectedResult = "Return 400 Warning",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Progress = Null array" }
            });
        }

        // -----------------------------------------------------------
        // GetWordleResultHandler_02 | A | Progress IsWon False -> Fail
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ProgressIsWonFalse_ShouldReturnFailure()
        {
            var p = new UserWordleProgress { DailyWordleId = "w1", IsWon = false };
            _miniGameMock.Setup(x => x.GetUserWordleProgressAsync("u", new[] { "w1" }, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<UserWordleProgress> { p });
            var handler = CreateHandler();
            var result = await handler.Handle(new GetWordleResultQuery { UserId = "u", DailyWordleId = "w1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("chi?n th?ng trò choi");

            QACollector.LogTestCase("MiniGame - Get Wordle Result", new TestCaseDetail
            {
                FunctionGroup = "GetWordleResultHandler",
                TestCaseID = "GetWordleResultHandler_02",
                Description = "Strict gameplay rule requires winning boolean flags valid avoiding cheating views effectively",
                ExpectedResult = "Return 400 Warning",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Progress IsWon = false" }
            });
        }

        // -----------------------------------------------------------
        // GetWordleResultHandler_03 | A | Game Null -> Fail
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_GameNull_ShouldReturnFailure()
        {
            var p = new UserWordleProgress { DailyWordleId = "w1", IsWon = true };
            _miniGameMock.Setup(x => x.GetUserWordleProgressAsync("u", new[] { "w1" }, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<UserWordleProgress> { p });
            _miniGameMock.Setup(x => x.GetDailyWordleByIdAsync("w1", It.IsAny<CancellationToken>())).ReturnsAsync((DailyWordle?)null);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetWordleResultQuery { UserId = "u", DailyWordleId = "w1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm th?y thông tin trò choi");

            QACollector.LogTestCase("MiniGame - Get Wordle Result", new TestCaseDetail
            {
                FunctionGroup = "GetWordleResultHandler",
                TestCaseID = "GetWordleResultHandler_03",
                Description = "Prevents corrupt states propagating mapping object",
                ExpectedResult = "Return GameNotFound 400",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetDailyWordleById = null" }
            });
        }

        // -----------------------------------------------------------
        // GetWordleResultHandler_04 | N | Completely Success Path maps securely fully nested object
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_SuccessMapping_ShouldMatch()
        {
            var p = new UserWordleProgress { DailyWordleId = "w1", IsWon = true, AttemptCount = 3 };
            var game = new DailyWordle { DailyWordleId = "w1", Word = "apple", VocabularyId = "v1" };
            var vocab = new Domain.Entities.Vocabulary { VocabularyId = "v1", Definition = "Fruit", ImgURL = "img", AudioURL = "aud" };
            
            _miniGameMock.Setup(x => x.GetUserWordleProgressAsync("u", new[] { "w1" }, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<UserWordleProgress> { p });
            _miniGameMock.Setup(x => x.GetDailyWordleByIdAsync("w1", It.IsAny<CancellationToken>())).ReturnsAsync(game);
            _vocabMock.Setup(x => x.GetByIdAsync("v1")).ReturnsAsync(vocab);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetWordleResultQuery { UserId = "u", DailyWordleId = "w1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Word.Should().Be("apple");
            result.Data.Definition.Should().Be("Fruit");
            result.Data.AudioUrl.Should().Be("aud");

            QACollector.LogTestCase("MiniGame - Get Wordle Result", new TestCaseDetail
            {
                FunctionGroup = "GetWordleResultHandler",
                TestCaseID = "GetWordleResultHandler_04",
                Description = "Valid extraction successfully maps correctly fully defined payload strings efficiently",
                ExpectedResult = "Success Model Mapped",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Fully populated Valid DB" }
            });
        }

        // -----------------------------------------------------------
        // GetWordleResultHandler_05 | N | Missing Visuals fallback safely empty strings
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_MissingVisuals_ShouldFallbackEmpty()
        {
            var p = new UserWordleProgress { DailyWordleId = "w1", IsWon = true, AttemptCount = 3 };
            var game = new DailyWordle { DailyWordleId = "w1", Word = "apple", VocabularyId = "v1" };
            var vocab = new Domain.Entities.Vocabulary { VocabularyId = "v1", Definition = null, ImgURL = null, AudioURL = null };
            
            _miniGameMock.Setup(x => x.GetUserWordleProgressAsync("u", new[] { "w1" }, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<UserWordleProgress> { p });
            _miniGameMock.Setup(x => x.GetDailyWordleByIdAsync("w1", It.IsAny<CancellationToken>())).ReturnsAsync(game);
            _vocabMock.Setup(x => x.GetByIdAsync("v1")).ReturnsAsync(vocab);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetWordleResultQuery { UserId = "u", DailyWordleId = "w1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Definition.Should().Be("");
            result.Data.ImageUrl.Should().Be("");

            QACollector.LogTestCase("MiniGame - Get Wordle Result", new TestCaseDetail
            {
                FunctionGroup = "GetWordleResultHandler",
                TestCaseID = "GetWordleResultHandler_05",
                Description = "Null coalescing fallback guarantees strictly unpopulated model strings avoiding frontend crashes dynamically natively",
                ExpectedResult = "Strings map empty explicitly safely",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Null definition and links" }
            });
        }

        // -----------------------------------------------------------
        // GetWordleResultHandler_06 | N | Verify Request Values strictly bind Database query calls
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_Bindings_ShouldPropagateValuesSpecifically()
        {
            var p = new UserWordleProgress { DailyWordleId = "wordle", IsWon = true };
            var game = new DailyWordle { DailyWordleId = "wordle", Word = "x", VocabularyId = "voc" };
            var vocab = new Domain.Entities.Vocabulary { VocabularyId = "voc" };
            
            _miniGameMock.Setup(x => x.GetUserWordleProgressAsync("user123", new[] { "wordle" }, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<UserWordleProgress> { p });
            _miniGameMock.Setup(x => x.GetDailyWordleByIdAsync("wordle", It.IsAny<CancellationToken>())).ReturnsAsync(game);
            _vocabMock.Setup(x => x.GetByIdAsync("voc")).ReturnsAsync(vocab);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetWordleResultQuery { UserId = "user123", DailyWordleId = "wordle" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _miniGameMock.Verify(x => x.GetUserWordleProgressAsync(It.Is<string>(y => y == "user123"), It.Is<string[]>(y => y[0] == "wordle"), It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("MiniGame - Get Wordle Result", new TestCaseDetail
            {
                FunctionGroup = "GetWordleResultHandler",
                TestCaseID = "GetWordleResultHandler_06",
                Description = "Execution strictly isolates exact object parameter boundaries passing downstream correctly seamlessly",
                ExpectedResult = "Called strictly binding properties",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Verify params mapped perfectly" }
            });
        }
    }
}
