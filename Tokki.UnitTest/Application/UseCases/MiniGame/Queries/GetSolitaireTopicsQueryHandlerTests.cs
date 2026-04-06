using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.Queries.Solitaire;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.MiniGame.Queries
{
    public class GetSolitaireTopicsQueryHandlerTests
    {
        private readonly Mock<IMiniGameRepository> _mockRepo;
        private readonly GetSolitaireTopicsQueryHandler _handler;

        public GetSolitaireTopicsQueryHandlerTests()
        {
            _mockRepo = new Mock<IMiniGameRepository>();
            _handler = new GetSolitaireTopicsQueryHandler(_mockRepo.Object);
        }

        // TC-MG-GST-01 | A | Requesting less than 20 -> Fail 400
        [Fact]
        public async Task Handle_LessThan20Request_ShouldFail400()
        {
            var query = new GetSolitaireTopicsQuery { Quantity = 19 };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("tối thiểu là 20");

            QACollector.LogTestCase("Mini Game - Get Solitaire Topics", new TestCaseDetail
            {
                FunctionGroup = "GetSolitaireTopicsQueryHandler",
                TestCaseID = "TC-MG-GST-01",
                Description = "Block bad thresholds enforcing mechanics accurately priorizing processing times",
                ExpectedResult = "400 Error threshold hit",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Quantity 19" }
            });
        }

        // TC-MG-GST-02 | A | Not Enough Total Output (< 20) -> Fail 400
        [Fact]
        public async Task Handle_TotalLessThan20System_ShouldFail400()
        {
            // Only 1 topic with 4 valid words
            var topic = new Topic 
            {
                TopicId = "T1",
                VocabularyTopics = new List<VocabularyTopic>
                {
                    new VocabularyTopic { Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = "v1" } },
                    new VocabularyTopic { Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = "v2" } },
                    new VocabularyTopic { Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = "v3" } },
                    new VocabularyTopic { Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = "v4" } }
                }
            };
            
            _mockRepo.Setup(x => x.GetSolitaireTopicsWithVocabsAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<Topic> { topic });

            var query = new GetSolitaireTopicsQuery { Quantity = 20 };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("hiện tại quá ít");

            QACollector.LogTestCase("Mini Game - Get Solitaire Topics", new TestCaseDetail
            {
                FunctionGroup = "GetSolitaireTopicsQueryHandler",
                TestCaseID = "TC-MG-GST-02",
                Description = "Forces 20 baseline system output unconditionally post-randomization checking limits directly",
                ExpectedResult = "400 Insufficient words",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Topic provides 4 words" }
            });
        }

        // TC-MG-GST-03 | A | Insufficient Words Margin Check -> Fail 400
        [Fact]
        public async Task Handle_GapGreaterThan5_ShouldFail400()
        {
            // Request 40 words, but system only yielded 30 words total
            var topics = new List<Topic>();
            for(int i = 0; i < 4; i++) 
            {
                var vp = new List<VocabularyTopic>();
                for(int j = 0; j < 8; j++) vp.Add(new VocabularyTopic { Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = "Vxxx" } });
                topics.Add(new Topic { TopicId = $"T{i}", VocabularyTopics = vp });
            }
            
            // Loop guarantees at most 4 * 8 = 32 items collected.
            _mockRepo.Setup(x => x.GetSolitaireTopicsWithVocabsAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(topics);

            var query = new GetSolitaireTopicsQuery { Quantity = 38 }; // Difference > 5 since collected <= 32
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Không đủ từ vựng theo yêu cầu");

            QACollector.LogTestCase("Mini Game - Get Solitaire Topics", new TestCaseDetail
            {
                FunctionGroup = "GetSolitaireTopicsQueryHandler",
                TestCaseID = "TC-MG-GST-03",
                Description = "Margin of error restriction caps off-limit parameters efficiently",
                ExpectedResult = "400 Difference Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Quantity - Collected > 5" }
            });
        }

        // TC-MG-GST-04 | N | Valid Generation Logic
        [Fact]
        public async Task Handle_SufficientTopics_ReturnsValidDTO()
        {
            var topics = new List<Topic>();
            for(int i = 0; i < 8; i++) 
            {
                var vp = new List<VocabularyTopic>();
                for(int j = 0; j < 8; j++) vp.Add(new VocabularyTopic { Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = $"V_{i}_{j}", Text = "Hi" } });
                topics.Add(new Topic { TopicId = $"T{i}", TopicName = "TName", VocabularyTopics = vp });
            }

            _mockRepo.Setup(x => x.GetSolitaireTopicsWithVocabsAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(topics);

            var query = new GetSolitaireTopicsQuery { Quantity = 20 };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeEmpty();

            QACollector.LogTestCase("Mini Game - Get Solitaire Topics", new TestCaseDetail
            {
                FunctionGroup = "GetSolitaireTopicsQueryHandler",
                TestCaseID = "TC-MG-GST-04",
                Description = "Standard workflow operates flawlessly collecting data blocks natively into grouped DTO arrays correctly",
                ExpectedResult = "Success DTO structure",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid system quantities mapping" }
            });
        }

        // TC-MG-GST-05 | N | Ignore Less than 4 Words Topics
        [Fact]
        public async Task Handle_TopicsWithLowWordCount_AreIgnored()
        {
            var topicLow = new Topic { TopicId = "T1", VocabularyTopics = new List<VocabularyTopic> 
                { new VocabularyTopic { Vocabulary = new Tokki.Domain.Entities.Vocabulary() } } };
            
            var topicHigh = new Topic { TopicId = "T2", VocabularyTopics = new List<VocabularyTopic>() };
            for(int j = 0; j < 25; j++) topicHigh.VocabularyTopics.Add(new VocabularyTopic { Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = $"A{j}" } });

            _mockRepo.Setup(x => x.GetSolitaireTopicsWithVocabsAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<Topic> { topicLow, topicHigh });

            var query = new GetSolitaireTopicsQuery { Quantity = 20 };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // T1 should be totally skipped, T2 must exist
            result.Data.Should().ContainSingle();
            result.Data[0].TopicId.Should().Be("T2");

            QACollector.LogTestCase("Mini Game - Get Solitaire Topics", new TestCaseDetail
            {
                FunctionGroup = "GetSolitaireTopicsQueryHandler",
                TestCaseID = "TC-MG-GST-05",
                Description = "Isolates inadequate vocabulary batches continuing looping sequence seamlessly without crashing bounds checking algorithms",
                ExpectedResult = "Only T2 returned output list 1",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "List includes a 1 item nested Topic element" }
            });
        }

        // TC-MG-GST-06 | N | Early Break Constraint Checked Post Increment
        [Fact]
        public async Task Handle_BreaksLoopEarlyOnLimitReach_ShouldStopIterating()
        {
            var validTopics = new List<Topic>();
            // 20 topics with 8 valid words each -> generating immense lists
            for(int i = 0; i < 20; i++) 
            {
                var vp = new List<VocabularyTopic>();
                for(int j = 0; j < 8; j++) vp.Add(new VocabularyTopic { Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = $"B" } });
                validTopics.Add(new Topic { TopicId = $"T_{i}", VocabularyTopics = vp });
            }

            _mockRepo.Setup(x => x.GetSolitaireTopicsWithVocabsAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(validTopics);

            var query = new GetSolitaireTopicsQuery { Quantity = 21 }; // Breaks fast
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Count.Should().BeLessThan(20);

            QACollector.LogTestCase("Mini Game - Get Solitaire Topics", new TestCaseDetail
            {
                FunctionGroup = "GetSolitaireTopicsQueryHandler",
                TestCaseID = "TC-MG-GST-06",
                Description = "Execution exits iterating quickly yielding optimized database response time bounds ensuring limited allocations",
                ExpectedResult = "Returned Array size < 20",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Break on limit" }
            });
        }
    }
}
