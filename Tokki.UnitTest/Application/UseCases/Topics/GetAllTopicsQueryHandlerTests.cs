using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Application.UseCases.Topics.Queries;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class GetAllTopicsQueryHandlerTests
    {
        private static Mock<ITopicRepository> GetRepoMock(
            IEnumerable<Topic>? items = null, int total = 0, int vocabCount = 3)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetVocabTopicsPagedAsync(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<TopicStatus?>(), It.IsAny<int?>()))
             .ReturnsAsync(((IEnumerable<Topic>)(items ?? new List<Topic>()), total));
            m.Setup(x => x.CountVocabulariesInTopicAsync(It.IsAny<string>())).ReturnsAsync(vocabCount);
            return m;
        }

        private static Mock<IEnumConfigRepository> GetEnumMock() => new Mock<IEnumConfigRepository>();

        private static GetAllTopicsQueryHandler CreateHandler(Mock<ITopicRepository>? repo = null, Mock<IEnumConfigRepository>? enumRepo = null)
            => new GetAllTopicsQueryHandler((repo ?? GetRepoMock()).Object, (enumRepo ?? GetEnumMock()).Object);

        private static GetAllTopicsQuery MakeQuery(int page = 1, int size = 10)
            => new GetAllTopicsQuery { PageNumber = page, PageSize = size };

        private static List<Topic> SampleTopics() => new List<Topic>
        {
            new Topic { TopicId = "T-001", TopicName = "Korean Basics",   Level = (int)TopicLevel.Level1,     Status = TopicStatus.Active,  OrderIndex = 1 },
            new Topic { TopicId = "T-002", TopicName = "Korean Intermediate", Level = (int)TopicLevel.Level3, Status = TopicStatus.Active,  OrderIndex = 2 },
        };

        // GetAllTopics_01 | N | Happy path: 2 topics → 200 PagedResult Count=2
        [Fact]
        public async Task Handle_RepoReturns2Topics_ShouldReturn200WithCount2()
        {
            var repo   = GetRepoMock(SampleTopics(), total: 2);
            var result = await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
            QACollector.LogTestCase("Topic - Get All", new TestCaseDetail { FunctionGroup = "GetAllTopics", TestCaseID = "GetAllTopics_01", Description = "2 topics → 200, Count=2, TotalCount=2", ExpectedResult = "IsSuccess=true, 200, Items.Count=2", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetVocabTopicsPagedAsync returns 2 items" } });
        }

        // GetAllTopics_02 | N | TopicDto fields correctly mapped
        [Fact]
        public async Task Handle_ReturnsTopics_DtoFieldsMappedCorrectly()
        {
            var repo   = GetRepoMock(SampleTopics(), total: 2, vocabCount: 5);
            var result = await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            var first  = result.Data!.Items[0];
            first.TopicId.Should().Be("T-001");
            first.TopicName.Should().Be("Korean Basics");
            first.Status.Should().Be(TopicStatus.Active);
            first.VocabularyCount.Should().Be(5);
            QACollector.LogTestCase("Topic - Get All", new TestCaseDetail { FunctionGroup = "GetAllTopics", TestCaseID = "GetAllTopics_02", Description = "DTO fields: Id, Name, Status, VocabularyCount=5 mapped", ExpectedResult = "All DTO fields correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "CountVocabulariesInTopicAsync returns 5" } });
        }

        // GetAllTopics_03 | B | GetVocabTopicsPagedAsync called with query params
        [Fact]
        public async Task Handle_WithParams_RepoCalledWithCorrectParams()
        {
            var repo = GetRepoMock();
            await CreateHandler(repo).Handle(new GetAllTopicsQuery { PageNumber = 2, PageSize = 5, Status = TopicStatus.Active, Level = (int)TopicLevel.Level1 }, CancellationToken.None);
            repo.Verify(x => x.GetVocabTopicsPagedAsync(2, 5, null, TopicStatus.Active, (int)TopicLevel.Level1), Times.Once);
            QACollector.LogTestCase("Topic - Get All", new TestCaseDetail { FunctionGroup = "GetAllTopics", TestCaseID = "GetAllTopics_03", Description = "GetVocabTopicsPagedAsync called with Page=2, Size=5, Status=Active, Level=Beginner", ExpectedResult = "Times.Once with correct params", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "All query params forwarded" } });
        }

        // GetAllTopics_04 | N | Empty repo → 200 with empty Items
        [Fact]
        public async Task Handle_NoTopics_ShouldReturn200WithEmptyItems()
        {
            var result = await CreateHandler(GetRepoMock(new List<Topic>(), 0)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
            QACollector.LogTestCase("Topic - Get All", new TestCaseDetail { FunctionGroup = "GetAllTopics", TestCaseID = "GetAllTopics_04", Description = "No topics → 200 with empty paged result", ExpectedResult = "Items=[], TotalCount=0", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No topics in system" } });
        }

        // GetAllTopics_05 | N | Paging metadata correct
        [Fact]
        public async Task Handle_Page3Size5Total50_PagingMetadataCorrect()
        {
            var repo   = GetRepoMock(SampleTopics(), total: 50);
            var result = await CreateHandler(repo).Handle(MakeQuery(page: 3, size: 5), CancellationToken.None);
            result.Data!.PageNumber.Should().Be(3);
            result.Data.PageSize.Should().Be(5);
            result.Data.TotalCount.Should().Be(50);
            QACollector.LogTestCase("Topic - Get All", new TestCaseDetail { FunctionGroup = "GetAllTopics", TestCaseID = "GetAllTopics_05", Description = "Page=3, Size=5, TotalCount=50 paging metadata correct", ExpectedResult = "Metadata correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Paging values in PagedResult" } });
        }

        // GetAllTopics_06 | B | CountVocabulariesInTopicAsync called once per topic
        [Fact]
        public async Task Handle_Returns2Topics_CountCalledTwice()
        {
            var repo = GetRepoMock(SampleTopics(), total: 2);
            await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            repo.Verify(x => x.CountVocabulariesInTopicAsync(It.IsAny<string>()), Times.Exactly(2));
            QACollector.LogTestCase("Topic - Get All", new TestCaseDetail { FunctionGroup = "GetAllTopics", TestCaseID = "GetAllTopics_06", Description = "CountVocabulariesInTopicAsync called once per topic (2 times)", ExpectedResult = "Times.Exactly(2)", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Vocab count fetched per topic in loop" } });
        }
    }
}