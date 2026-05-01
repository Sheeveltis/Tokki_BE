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
using Tokki.Application.UseCases.Topics.Queries.GetTopicForUser;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class GetAllTopicsForUserQueryHandlerTests
    {
        private static Mock<ITopicRepository> GetTopicMock(
            List<Topic>? items = null, int total = 0, int vocabCount = 5, int learnedCount = 0)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetVocabTopicsPagedForUserAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<int?>()))
             .ReturnsAsync((items ?? new List<Topic>(), total));
            m.Setup(x => x.CountVocabulariesInTopicAsync(It.IsAny<string>())).ReturnsAsync(vocabCount);
            m.Setup(x => x.CountLearnedVocabulariesAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(learnedCount);
            return m;
        }

        private static Mock<IUserTopicProgressRepository> GetProgressMock()
            => new Mock<IUserTopicProgressRepository>();

        private static Mock<IEnumConfigRepository> GetEnumMock() => new Mock<IEnumConfigRepository>();

        private static GetAllTopicsForUserQueryHandler CreateHandler(
            Mock<ITopicRepository>?              topicRepo    = null,
            Mock<IUserTopicProgressRepository>?  progressRepo = null,
            Mock<IEnumConfigRepository>?         enumRepo     = null)
            => new GetAllTopicsForUserQueryHandler(
                (topicRepo    ?? GetTopicMock()).Object,
                (progressRepo ?? GetProgressMock()).Object,
                (enumRepo     ?? GetEnumMock()).Object);

        private static List<Topic> SampleTopics(int count = 2)
        {
            var list = new List<Topic>();
            for (int i = 1; i <= count; i++)
                list.Add(new Topic { TopicId = $"T-{i:000}", TopicName = $"Topic {i}", Level = (int)TopicLevel.Level1, Status = TopicStatus.Active });
            return list;
        }

        private static GetAllTopicsForUserQuery MakeQuery(string userId = "U-001", int page = 1, int size = 10) =>
            new GetAllTopicsForUserQuery { UserId = userId, PageNumber = page, PageSize = size };

        // GetAllTopicsForUser_01 | N | Happy path: 2 topics → 200, Items.Count=2
        [Fact]
        public async Task Handle_TwoTopics_ShouldReturn200WithCount2()
        {
            var repo   = GetTopicMock(SampleTopics(2), total: 2);
            var result = await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
            QACollector.LogTestCase("Topic - Get For User", new TestCaseDetail { FunctionGroup = "GetAllTopicsForUser", TestCaseID = "GetAllTopicsForUser_01", Description = "2 topics → 200, Items.Count=2, TotalCount=2", ExpectedResult = "IsSuccess=true, 200", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetVocabTopicsPagedForUserAsync returns 2" } });
        }

        // GetAllTopicsForUser_02 | N | Progress 5/10 → Progress=50, IsLearned=false
        [Fact]
        public async Task Handle_5Of10Learned_ProgressIs50AndNotLearned()
        {
            var repo   = GetTopicMock(SampleTopics(1), total: 1, vocabCount: 10, learnedCount: 5);
            var result = await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            var dto    = result.Data!.Items[0];
            dto.Progress.Should().Be(50);
            dto.IsLearned.Should().BeFalse();
            QACollector.LogTestCase("Topic - Get For User", new TestCaseDetail { FunctionGroup = "GetAllTopicsForUser", TestCaseID = "GetAllTopicsForUser_02", Description = "5/10 learned → Progress=50, IsLearned=false", ExpectedResult = "Progress=50, IsLearned=false", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "learnedCount/totalVocab=50%" } });
        }

        // GetAllTopicsForUser_03 | N | All 10/10 learned → Progress=100, IsLearned=true
        [Fact]
        public async Task Handle_AllLearned_ProgressIs100AndIsLearnedTrue()
        {
            var repo   = GetTopicMock(SampleTopics(1), total: 1, vocabCount: 10, learnedCount: 10);
            var result = await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            var dto    = result.Data!.Items[0];
            dto.Progress.Should().Be(100);
            dto.IsLearned.Should().BeTrue();
            QACollector.LogTestCase("Topic - Get For User", new TestCaseDetail { FunctionGroup = "GetAllTopicsForUser", TestCaseID = "GetAllTopicsForUser_03", Description = "10/10 learned → Progress=100, IsLearned=true", ExpectedResult = "Progress=100, IsLearned=true", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "learnedCount>=totalVocab" } });
        }

        // GetAllTopicsForUser_04 | N | Empty list → 200 with empty Items
        [Fact]
        public async Task Handle_NoTopics_ShouldReturn200WithEmptyItems()
        {
            var result = await CreateHandler(GetTopicMock(new List<Topic>(), total: 0)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            QACollector.LogTestCase("Topic - Get For User", new TestCaseDetail { FunctionGroup = "GetAllTopicsForUser", TestCaseID = "GetAllTopicsForUser_04", Description = "No topics → 200, empty Items", ExpectedResult = "IsSuccess=true, Items=[]", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No active topics for user" } });
        }

        // GetAllTopicsForUser_05 | N | Paging metadata correct
        [Fact]
        public async Task Handle_Page2Size5Total20_PagingMetadataCorrect()
        {
            var repo   = GetTopicMock(SampleTopics(2), total: 20);
            var result = await CreateHandler(repo).Handle(MakeQuery(page: 2, size: 5), CancellationToken.None);
            result.Data!.PageNumber.Should().Be(2);
            result.Data.PageSize.Should().Be(5);
            result.Data.TotalCount.Should().Be(20);
            QACollector.LogTestCase("Topic - Get For User", new TestCaseDetail { FunctionGroup = "GetAllTopicsForUser", TestCaseID = "GetAllTopicsForUser_05", Description = "Page=2, Size=5, Total=20 paging metadata correct", ExpectedResult = "Metadata correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Paging values in PagedResult" } });
        }

        // GetAllTopicsForUser_06 | B | GetVocabTopicsPagedForUserAsync called with correct params
        [Fact]
        public async Task Handle_WithParams_RepoCalledWithCorrectParams()
        {
            var repo = GetTopicMock();
            await CreateHandler(repo).Handle(new GetAllTopicsForUserQuery { UserId = "U-001", PageNumber = 3, PageSize = 8, Level =(int) TopicLevel.Level3 }, CancellationToken.None);
            repo.Verify(x => x.GetVocabTopicsPagedForUserAsync(3, 8, null, (int)TopicLevel.Level3), Times.Once);
            QACollector.LogTestCase("Topic - Get For User", new TestCaseDetail { FunctionGroup = "GetAllTopicsForUser", TestCaseID = "GetAllTopicsForUser_06", Description = "RepoAsync called with Page=3, Size=8, Level=Level3", ExpectedResult = "Times.Once with correct params", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "All query params forwarded" } });
        }
    }
}