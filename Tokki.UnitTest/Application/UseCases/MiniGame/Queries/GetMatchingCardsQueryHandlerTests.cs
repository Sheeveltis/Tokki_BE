using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.Queries.MatchingCard;
using Tokki.Application.UseCases.MiniGame.DTOs;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.MiniGame.Queries
{
    public class GetMatchingCardsQueryHandlerTests
    {
        private readonly Mock<IMiniGameRepository> _mockRepo;
        private readonly GetMatchingCardsQueryHandler _handler;

        public GetMatchingCardsQueryHandlerTests()
        {
            _mockRepo = new Mock<IMiniGameRepository>();
            _handler = new GetMatchingCardsQueryHandler(_mockRepo.Object);
        }

        // GetMatchingCardsQueryHandler_01 | A | Vocab Collection Null -> Error
        [Fact]
        public async Task Handle_NullVocabulary_ShouldReturn404()
        {
            _mockRepo.Setup(x => x.GetRandomVocabulariesByTopicAsync("top1", 10, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((List<Tokki.Domain.Entities.Vocabulary>)null);

            var query = new GetMatchingCardsQuery { TopicId = "top1", Quantity = 10 };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Mini Game - Get Matching Cards", new TestCaseDetail
            {
                FunctionGroup = "GetMatchingCardsQueryHandler",
                TestCaseID = "GetMatchingCardsQueryHandler_01",
                Description = "Empty subsets accurately parsed directly returning graceful exits",
                ExpectedResult = "Error 404",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repo gives Null" }
            });
        }

        // GetMatchingCardsQueryHandler_02 | A | Vocab Collection Empty -> Error
        [Fact]
        public async Task Handle_EmptyVocabulary_ShouldReturn404()
        {
            _mockRepo.Setup(x => x.GetRandomVocabulariesByTopicAsync("top1", 10, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary>());

            var query = new GetMatchingCardsQuery { TopicId = "top1", Quantity = 10 };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Mini Game - Get Matching Cards", new TestCaseDetail
            {
                FunctionGroup = "GetMatchingCardsQueryHandler",
                TestCaseID = "GetMatchingCardsQueryHandler_02",
                Description = "Validates Any() condition blocks cleanly",
                ExpectedResult = "Error 404",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repo gives empty List" }
            });
        }

        // GetMatchingCardsQueryHandler_03 | N | Standard Result Maps DTO Properties
        [Fact]
        public async Task Handle_ValidResult_ShouldMapDTOs()
        {
            var list = new List<Tokki.Domain.Entities.Vocabulary>
            {
                new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V1", Text = "Apple", ImgURL = "app.jpg" }
            };
            _mockRepo.Setup(x => x.GetRandomVocabulariesByTopicAsync("top1", 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(list);

            var query = new GetMatchingCardsQuery { TopicId = "top1", Quantity = 1 };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data[0].Text.Should().Be("Apple");
            result.Data[0].ImgURL.Should().Be("app.jpg");

            QACollector.LogTestCase("Mini Game - Get Matching Cards", new TestCaseDetail
            {
                FunctionGroup = "GetMatchingCardsQueryHandler",
                TestCaseID = "GetMatchingCardsQueryHandler_03",
                Description = "Verifies accurate data mapping across domain object borders into standard shapes",
                ExpectedResult = "Success Data Output",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "1 Item Return" }
            });
        }

        // GetMatchingCardsQueryHandler_04 | N | Preserves Pronunciations
        [Fact]
        public async Task Handle_PreservesPronunciation_MapsValidString()
        {
            var list = new List<Tokki.Domain.Entities.Vocabulary>
            {
                new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V1", Text = "Car", Pronunciation = "[Kaa]" }
            };
            _mockRepo.Setup(x => x.GetRandomVocabulariesByTopicAsync("top1", 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(list);

            var query = new GetMatchingCardsQuery { TopicId = "top1", Quantity = 1 };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data[0].Pronunciation.Should().Be("[Kaa]");

            QACollector.LogTestCase("Mini Game - Get Matching Cards", new TestCaseDetail
            {
                FunctionGroup = "GetMatchingCardsQueryHandler",
                TestCaseID = "GetMatchingCardsQueryHandler_04",
                Description = "Pronunciations map identically",
                ExpectedResult = "Matched property string",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Pronunciation Data Available" }
            });
        }

        // GetMatchingCardsQueryHandler_05 | N | Preserves Definitions
        [Fact]
        public async Task Handle_PreservesDefinitions_MapsValidString()
        {
            var list = new List<Tokki.Domain.Entities.Vocabulary>
            {
                new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V1", Text = "Dog", Definition = "A good boy" }
            };
            _mockRepo.Setup(x => x.GetRandomVocabulariesByTopicAsync("top1", 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(list);

            var query = new GetMatchingCardsQuery { TopicId = "top1", Quantity = 1 };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data[0].Definition.Should().Be("A good boy");

            QACollector.LogTestCase("Mini Game - Get Matching Cards", new TestCaseDetail
            {
                FunctionGroup = "GetMatchingCardsQueryHandler",
                TestCaseID = "GetMatchingCardsQueryHandler_05",
                Description = "Definitions map identically maintaining vocabulary relationships",
                ExpectedResult = "Matched property string definition",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Definition Data Available" }
            });
        }

        // GetMatchingCardsQueryHandler_06 | N | Returns proper list lengths directly matching logic parameters
        [Fact]
        public async Task Handle_LongList_MatchesInputCount()
        {
            var list = new List<Tokki.Domain.Entities.Vocabulary>();
            for(int i = 0; i < 20; i++) list.Add(new Tokki.Domain.Entities.Vocabulary { VocabularyId = $"V{i}", Text = $"T{i}" });

            _mockRepo.Setup(x => x.GetRandomVocabulariesByTopicAsync("top2", 20, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(list);

            var query = new GetMatchingCardsQuery { TopicId = "top2", Quantity = 20 };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data.Should().HaveCount(20);

            QACollector.LogTestCase("Mini Game - Get Matching Cards", new TestCaseDetail
            {
                FunctionGroup = "GetMatchingCardsQueryHandler",
                TestCaseID = "GetMatchingCardsQueryHandler_06",
                Description = "Length collections persist through Select projections safely tracking arrays sequentially",
                ExpectedResult = "Returns Count 20 list elements correctly",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "20 Items processing iteration" }
            });
        }
    }
}
