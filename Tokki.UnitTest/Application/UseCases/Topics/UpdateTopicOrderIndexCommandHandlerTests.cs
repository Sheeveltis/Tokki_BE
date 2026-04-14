using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopicOrderIndex;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class UpdateTopicOrderIndexCommandHandlerTests
    {
        private static Mock<ITopicRepository> GetRepoMock(Topic? topic = null)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(topic);
            m.Setup(x => x.UpdateAsync(It.IsAny<Topic>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            m.Setup(x => x.ShiftOrderIndexBetweenAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TopicType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
            m.Setup(x => x.ShiftOrderIndexUpFromAsync(It.IsAny<int>(), It.IsAny<TopicType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
            return m;
        }

        private static UpdateTopicOrderIndexCommandHandler CreateHandler(Mock<ITopicRepository>? repo = null)
            => new UpdateTopicOrderIndexCommandHandler(
                (repo ?? GetRepoMock()).Object,
                NullLogger<UpdateTopicOrderIndexCommandHandler>.Instance);

        private static Topic SampleTopic(int? orderIndex = 1) =>
            new Topic { TopicId = "T-001", TopicName = "Korean", Status = TopicStatus.Active, TopicType = TopicType.VocabStudy, OrderIndex = orderIndex };

        private static UpdateTopicOrderIndexCommand MakeCmd(string id = "T-001", int index = 3, string updatedBy = "U-001") =>
            new UpdateTopicOrderIndexCommand { TopicId = id, OrderIndex = index, UpdatedBy = updatedBy };

        // TC-TOPIC-ORD-01 | A | Topic not found → 404
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(GetRepoMock(null)).Handle(MakeCmd("MISSING"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Topic - Update Order Index", new TestCaseDetail { FunctionGroup = "UpdateTopicOrderIndex", TestCaseID = "TC-TOPIC-ORD-01", Description = "Topic not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // TC-TOPIC-ORD-02 | A | Topic deleted → 409
        [Fact]
        public async Task Handle_TopicDeleted_ShouldReturn409()
        {
            var topic = SampleTopic(1);
            topic.Status = TopicStatus.Deleted;
            var result = await CreateHandler(GetRepoMock(topic)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            QACollector.LogTestCase("Topic - Update Order Index", new TestCaseDetail { FunctionGroup = "UpdateTopicOrderIndex", TestCaseID = "TC-TOPIC-ORD-02", Description = "Topic deleted → 409", ExpectedResult = "IsSuccess=false, 409", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Status == Deleted" } });
        }

        // TC-TOPIC-ORD-03 | N | Same index → idempotent 200
        [Fact]
        public async Task Handle_SameOrderIndex_ShouldReturn200Idempotent()
        {
            var result = await CreateHandler(GetRepoMock(SampleTopic(3))).Handle(MakeCmd(index: 3), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            QACollector.LogTestCase("Topic - Update Order Index", new TestCaseDetail { FunctionGroup = "UpdateTopicOrderIndex", TestCaseID = "TC-TOPIC-ORD-03", Description = "Same index → idempotent 200", ExpectedResult = "IsSuccess=true, 200", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "oldIndex == newIndex" } });
        }

        // TC-TOPIC-ORD-04 | N | Happy path: new index set on topic
        [Fact]
        public async Task Handle_ValidRequest_TopicOrderIndexUpdated()
        {
            var topic  = SampleTopic(1);
            var result = await CreateHandler(GetRepoMock(topic)).Handle(MakeCmd(index: 5), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            topic.OrderIndex.Should().Be(5);
            QACollector.LogTestCase("Topic - Update Order Index", new TestCaseDetail { FunctionGroup = "UpdateTopicOrderIndex", TestCaseID = "TC-TOPIC-ORD-04", Description = "Valid request → OrderIndex=5, 200", ExpectedResult = "IsSuccess=true, 200, OrderIndex=5", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "OrderIndex mutated" } });
        }

        // TC-TOPIC-ORD-05 | B | ShiftOrderIndexBetweenAsync called when old index exists
        [Fact]
        public async Task Handle_HasOldIndex_ShiftBetweenCalled()
        {
            var repo = GetRepoMock(SampleTopic(2));
            await CreateHandler(repo).Handle(MakeCmd(index: 5), CancellationToken.None);
            repo.Verify(x => x.ShiftOrderIndexBetweenAsync(2, 5, It.IsAny<TopicType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
            QACollector.LogTestCase("Topic - Update Order Index", new TestCaseDetail { FunctionGroup = "UpdateTopicOrderIndex", TestCaseID = "TC-TOPIC-ORD-05", Description = "Has old index → ShiftBetween(2→5) called", ExpectedResult = "Times.Once with (2, 5)", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "oldIndex=2, newIndex=5" } });
        }

        // TC-TOPIC-ORD-06 | A | Repository throws → 500
        [Fact]
        public async Task Handle_RepoThrows_ShouldReturn500()
        {
            var repo = new Mock<ITopicRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB fail"));
            var result = await CreateHandler(repo).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            QACollector.LogTestCase("Topic - Update Order Index", new TestCaseDetail { FunctionGroup = "UpdateTopicOrderIndex", TestCaseID = "TC-TOPIC-ORD-06", Description = "Repository throws → 500", ExpectedResult = "IsSuccess=false, 500", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Exception caught" } });
        }
    }
}