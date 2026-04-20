using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Application.UseCases.Topics.Queries.GetById;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class GetTopicDetailByIdQueryHandlerTests
    {
        private static Mock<ITopicRepository> GetTopicMock(Topic? topic = null)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(topic);
            return m;
        }

        private static Mock<IVocabularyTopicRepository> GetVtMock(List<VocabularyTopic>? vts = null)
        {
            var m = new Mock<IVocabularyTopicRepository>();
            m.Setup(x => x.GetByTopicIdAsync(It.IsAny<string>())).ReturnsAsync(vts ?? new List<VocabularyTopic>());
            return m;
        }

        private static GetTopicDetailByIdQueryHandler CreateHandler(
            Mock<ITopicRepository>?           topicRepo = null,
            Mock<IVocabularyTopicRepository>? vtRepo    = null)
            => new GetTopicDetailByIdQueryHandler(
                (topicRepo ?? GetTopicMock()).Object,
                (vtRepo    ?? GetVtMock()).Object);

        private static Topic SampleTopic() => new Topic
        {
            TopicId   = "T-001", TopicName = "Korean Basics", Description = "Intro topic",
            Level     = TopicLevel.Level1, Status = TopicStatus.Active, OrderIndex = 1
        };

        private static List<VocabularyTopic> SampleVtWithActiveVocab() => new List<VocabularyTopic>
        {
            new VocabularyTopic
            {
                Status     = VocabularyTopicStatus.Active,
                Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V-001", Text = "안녕", Status = VocabularyStatus.Active }
            }
        };

        // GetTopicDetailById_01 | A | Topic not found → 404
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(GetTopicMock(null)).Handle(new GetTopicDetailByIdQuery { TopicId = "MISSING" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Topic - Get Detail By Id", new TestCaseDetail { FunctionGroup = "GetTopicDetailById", TestCaseID = "GetTopicDetailById_01", Description = "Topic not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // GetTopicDetailById_02 | N | Happy path → 200 with TopicDetailDto
        [Fact]
        public async Task Handle_TopicFound_ShouldReturn200WithDto()
        {
            var result = await CreateHandler(GetTopicMock(SampleTopic()), GetVtMock(SampleVtWithActiveVocab()))
                .Handle(new GetTopicDetailByIdQuery { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.TopicId.Should().Be("T-001");
            QACollector.LogTestCase("Topic - Get Detail By Id", new TestCaseDetail { FunctionGroup = "GetTopicDetailById", TestCaseID = "GetTopicDetailById_02", Description = "Valid request → 200, Data.TopicId='T-001'", ExpectedResult = "IsSuccess=true, 200", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Topic found, mappings returned" } });
        }

        // GetTopicDetailById_03 | N | Only Active vocabulary mappings included in Vocabularies list
        [Fact]
        public async Task Handle_MixedVtStatuses_OnlyActiveVocabsIncluded()
        {
            var vts = new List<VocabularyTopic>
            {
                new VocabularyTopic { Status = VocabularyTopicStatus.Active,  Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V-001", Status = VocabularyStatus.Active } },
                new VocabularyTopic { Status = VocabularyTopicStatus.Deleted, Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V-002", Status = VocabularyStatus.Active } },
                new VocabularyTopic { Status = VocabularyTopicStatus.Active,  Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V-003", Status = VocabularyStatus.Deleted } }
            };
            var result = await CreateHandler(GetTopicMock(SampleTopic()), GetVtMock(vts))
                .Handle(new GetTopicDetailByIdQuery { TopicId = "T-001" }, CancellationToken.None);
            result.Data!.Vocabularies.Should().HaveCount(1);
            result.Data.Vocabularies[0].VocabularyId.Should().Be("V-001");
            QACollector.LogTestCase("Topic - Get Detail By Id", new TestCaseDetail { FunctionGroup = "GetTopicDetailById", TestCaseID = "GetTopicDetailById_03", Description = "Only VtStatus=Active AND VocabStatus=Active included", ExpectedResult = "Vocabularies.Count=1 (only V-001)", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Both status filters applied" } });
        }

        // GetTopicDetailById_04 | N | VocabularyCount equals active vocab count
        [Fact]
        public async Task Handle_TwoActiveVocabs_VocabularyCountEquals2()
        {
            var vts = new List<VocabularyTopic>
            {
                new VocabularyTopic { Status = VocabularyTopicStatus.Active, Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V-001", Status = VocabularyStatus.Active } },
                new VocabularyTopic { Status = VocabularyTopicStatus.Active, Vocabulary = new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V-002", Status = VocabularyStatus.Active } }
            };
            var result = await CreateHandler(GetTopicMock(SampleTopic()), GetVtMock(vts))
                .Handle(new GetTopicDetailByIdQuery { TopicId = "T-001" }, CancellationToken.None);
            result.Data!.VocabularyCount.Should().Be(2);
            QACollector.LogTestCase("Topic - Get Detail By Id", new TestCaseDetail { FunctionGroup = "GetTopicDetailById", TestCaseID = "GetTopicDetailById_04", Description = "VocabularyCount=2 (active vocab count)", ExpectedResult = "VocabularyCount=2", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "VocabularyCount = activeVocabs.Count" } });
        }

        // GetTopicDetailById_05 | N | Topic metadata fields mapped correctly
        [Fact]
        public async Task Handle_TopicFound_MetadataFieldsMappedCorrectly()
        {
            var result = await CreateHandler(GetTopicMock(SampleTopic()), GetVtMock())
                .Handle(new GetTopicDetailByIdQuery { TopicId = "T-001" }, CancellationToken.None);
            result.Data!.TopicName.Should().Be("Korean Basics");
            result.Data.Level.Should().Be(TopicLevel.Level1);
            result.Data.Status.Should().Be(TopicStatus.Active);
            result.Data.OrderIndex.Should().Be(1);
            QACollector.LogTestCase("Topic - Get Detail By Id", new TestCaseDetail { FunctionGroup = "GetTopicDetailById", TestCaseID = "GetTopicDetailById_05", Description = "TopicName, Level, Status, OrderIndex mapped correctly", ExpectedResult = "All fields correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Topic entity mapped to TopicDetailDto" } });
        }

        // GetTopicDetailById_06 | B | GetByTopicIdAsync called with correct topicId
        [Fact]
        public async Task Handle_TopicFound_VtRepoCalledWithCorrectTopicId()
        {
            var vtRepo = GetVtMock();
            await CreateHandler(GetTopicMock(SampleTopic()), vtRepo).Handle(new GetTopicDetailByIdQuery { TopicId = "T-001" }, CancellationToken.None);
            vtRepo.Verify(x => x.GetByTopicIdAsync("T-001"), Times.Once);
            QACollector.LogTestCase("Topic - Get Detail By Id", new TestCaseDetail { FunctionGroup = "GetTopicDetailById", TestCaseID = "GetTopicDetailById_06", Description = "GetByTopicIdAsync called with 'T-001'", ExpectedResult = "Times.Once with correct ID", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TopicId forwarded to VT repo" } });
        }
    }
}