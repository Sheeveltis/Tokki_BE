using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Application.UseCases.Topics.Queries.CheckTopicCompletion;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class CheckTopicCompletionQueryHandlerTests
    {
        private static Mock<ITopicRepository> GetRepoMock(Topic? topic = null, int totalVocab = 10, int learned = 0)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(topic);
            m.Setup(x => x.CountVocabulariesInTopicAsync(It.IsAny<string>())).ReturnsAsync(totalVocab);
            m.Setup(x => x.CountLearnedVocabulariesAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(learned);
            return m;
        }

        private static CheckTopicCompletionQueryHandler CreateHandler(Mock<ITopicRepository>? repo = null)
            => new CheckTopicCompletionQueryHandler((repo ?? GetRepoMock()).Object);

        private static Topic SampleTopic() =>
            new Topic { TopicId = "T-001", TopicName = "Korean Basics" };

        private static CheckTopicCompletionQuery MakeQuery(string topicId = "T-001", string userId = "U-001") =>
            new CheckTopicCompletionQuery { TopicId = topicId, UserId = userId };

        // TC-TOPIC-COMP-01 | A | Topic not found → 404
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(GetRepoMock(null)).Handle(MakeQuery("MISSING"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Topic - Check Completion", new TestCaseDetail { FunctionGroup = "CheckTopicCompletion", TestCaseID = "TC-TOPIC-COMP-01", Description = "Topic not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // TC-TOPIC-COMP-02 | N | Topic with 0 vocab → IsCompleted=true, Progress=100
        [Fact]
        public async Task Handle_TopicWithNoVocabs_ShouldReturnCompletedWith100Percent()
        {
            var result = await CreateHandler(GetRepoMock(SampleTopic(), totalVocab: 0)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.IsCompleted.Should().BeTrue();
            result.Data.ProgressPercent.Should().Be(100);
            result.Data.TotalVocab.Should().Be(0);
            QACollector.LogTestCase("Topic - Check Completion", new TestCaseDetail { FunctionGroup = "CheckTopicCompletion", TestCaseID = "TC-TOPIC-COMP-02", Description = "No vocabs → IsCompleted=true, Progress=100", ExpectedResult = "IsCompleted=true, ProgressPercent=100", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "totalVocab=0 → complete" } });
        }

        // TC-TOPIC-COMP-03 | N | 5/10 vocabs learned → Progress=50, IsCompleted=false
        [Fact]
        public async Task Handle_5Of10Learned_ShouldReturn50PercentNotCompleted()
        {
            var result = await CreateHandler(GetRepoMock(SampleTopic(), totalVocab: 10, learned: 5)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.ProgressPercent.Should().Be(50);
            result.Data.IsCompleted.Should().BeFalse();
            result.Data.LearnedVocab.Should().Be(5);
            QACollector.LogTestCase("Topic - Check Completion", new TestCaseDetail { FunctionGroup = "CheckTopicCompletion", TestCaseID = "TC-TOPIC-COMP-03", Description = "5/10 learned → ProgressPercent=50, IsCompleted=false", ExpectedResult = "ProgressPercent=50, IsCompleted=false", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "learned/total=50%" } });
        }

        // TC-TOPIC-COMP-04 | N | All 10/10 vocabs learned → IsCompleted=true, Progress=100
        [Fact]
        public async Task Handle_AllLearned_ShouldReturnCompletedWith100Percent()
        {
            var result = await CreateHandler(GetRepoMock(SampleTopic(), totalVocab: 10, learned: 10)).Handle(MakeQuery(), CancellationToken.None);
            result.Data!.IsCompleted.Should().BeTrue();
            result.Data.ProgressPercent.Should().Be(100);
            QACollector.LogTestCase("Topic - Check Completion", new TestCaseDetail { FunctionGroup = "CheckTopicCompletion", TestCaseID = "TC-TOPIC-COMP-04", Description = "10/10 learned → IsCompleted=true, Progress=100", ExpectedResult = "IsCompleted=true, ProgressPercent=100", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "learnedCount>=totalVocab" } });
        }

        // TC-TOPIC-COMP-05 | N | TopicId echoed in DTO
        [Fact]
        public async Task Handle_TopicFound_TopicIdEchoedInDto()
        {
            var result = await CreateHandler(GetRepoMock(SampleTopic(), totalVocab: 10, learned: 3)).Handle(MakeQuery("T-001"), CancellationToken.None);
            result.Data!.TopicId.Should().Be("T-001");
            QACollector.LogTestCase("Topic - Check Completion", new TestCaseDetail { FunctionGroup = "CheckTopicCompletion", TestCaseID = "TC-TOPIC-COMP-05", Description = "TopicId='T-001' echoed in result DTO", ExpectedResult = "Data.TopicId='T-001'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TopicId mapped to DTO" } });
        }

        // TC-TOPIC-COMP-06 | B | CountLearnedVocabulariesAsync called with correct userId and topicId
        [Fact]
        public async Task Handle_TopicFound_CountLearnedCalledWithCorrectIds()
        {
            var repo = GetRepoMock(SampleTopic(), totalVocab: 5, learned: 2);
            await CreateHandler(repo).Handle(MakeQuery("T-001", "U-XYZ"), CancellationToken.None);
            repo.Verify(x => x.CountLearnedVocabulariesAsync("U-XYZ", "T-001"), Times.Once);
            QACollector.LogTestCase("Topic - Check Completion", new TestCaseDetail { FunctionGroup = "CheckTopicCompletion", TestCaseID = "TC-TOPIC-COMP-06", Description = "CountLearnedVocabulariesAsync called with userId='U-XYZ', topicId='T-001'", ExpectedResult = "Times.Once with correct IDs", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Both IDs forwarded to repo" } });
        }
    }
}