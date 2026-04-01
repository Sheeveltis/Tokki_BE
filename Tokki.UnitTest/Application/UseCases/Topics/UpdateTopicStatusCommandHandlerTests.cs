using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopicStatus;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class UpdateTopicStatusCommandHandlerTests
    {
        private static Mock<ITopicRepository> GetRepoMock(Topic? topic = null)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(topic);
            m.Setup(x => x.UpdateAsync(It.IsAny<Topic>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
        }

        private static Mock<IVocabularyTopicRepository> GetVtMock()
        {
            var m = new Mock<IVocabularyTopicRepository>();
            m.Setup(x => x.GetByTopicIdAsync(It.IsAny<string>())).ReturnsAsync(new List<VocabularyTopic>());
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));
            return m;
        }

        private static UpdateTopicStatusCommandHandler CreateHandler(
            Mock<ITopicRepository>?           topicRepo = null,
            Mock<IVocabularyTopicRepository>? vtRepo    = null)
            => new UpdateTopicStatusCommandHandler(
                (topicRepo ?? GetRepoMock()).Object,
                (vtRepo    ?? GetVtMock()).Object,
                NullLogger<UpdateTopicStatusCommandHandler>.Instance);

        private static Topic SampleTopic(TopicStatus status) =>
            new Topic { TopicId = "T-001", TopicName = "Korean", Status = status };

        private static UpdateTopicStatusCommand MakeCmd(string id = "T-001", TopicStatus status = TopicStatus.Active) =>
            new UpdateTopicStatusCommand { TopicId = id, Status = status, UpdatedBy = "U-001" };

        // TC-TOPIC-STS-01 | A | Topic not found → 404
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(GetRepoMock(null)).Handle(MakeCmd("MISSING"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Topic - Update Status", new TestCaseDetail { FunctionGroup = "UpdateTopicStatus", TestCaseID = "TC-TOPIC-STS-01", Description = "Topic not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // TC-TOPIC-STS-02 | A | Topic already deleted → 409
        [Fact]
        public async Task Handle_TopicAlreadyDeleted_ShouldReturn409()
        {
            var result = await CreateHandler(GetRepoMock(SampleTopic(TopicStatus.Deleted))).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            QACollector.LogTestCase("Topic - Update Status", new TestCaseDetail { FunctionGroup = "UpdateTopicStatus", TestCaseID = "TC-TOPIC-STS-02", Description = "Topic already deleted → 409", ExpectedResult = "IsSuccess=false, 409", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Status == Deleted" } });
        }

        // TC-TOPIC-STS-03 | N | Happy path: Draft → Active, return 200
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn200()
        {
            var result = await CreateHandler(GetRepoMock(SampleTopic(TopicStatus.Draft))).Handle(MakeCmd(status: TopicStatus.Active), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            QACollector.LogTestCase("Topic - Update Status", new TestCaseDetail { FunctionGroup = "UpdateTopicStatus", TestCaseID = "TC-TOPIC-STS-03", Description = "Draft→Active update → 200", ExpectedResult = "IsSuccess=true, 200", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Status changed" } });
        }

        // TC-TOPIC-STS-04 | N | Same status → 200 (no-op)
        [Fact]
        public async Task Handle_SameStatus_ShouldReturn200NoOp()
        {
            var topic  = SampleTopic(TopicStatus.Active);
            var repo   = GetRepoMock(topic);
            var result = await CreateHandler(repo).Handle(MakeCmd(status: TopicStatus.Active), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            // Status unchanged
            topic.Status.Should().Be(TopicStatus.Active);
            QACollector.LogTestCase("Topic - Update Status", new TestCaseDetail { FunctionGroup = "UpdateTopicStatus", TestCaseID = "TC-TOPIC-STS-04", Description = "Same status update → 200 no-op", ExpectedResult = "IsSuccess=true, 200, Status unchanged", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "newStatus == existingStatus" } });
        }

        // TC-TOPIC-STS-05 | B | UpdateAsync and SaveChangesAsync called once
        [Fact]
        public async Task Handle_ValidRequest_UpdateAndSaveCalledOnce()
        {
            var repo = GetRepoMock(SampleTopic(TopicStatus.Draft));
            await CreateHandler(repo).Handle(MakeCmd(), CancellationToken.None);
            repo.Verify(x => x.UpdateAsync(It.IsAny<Topic>()), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Topic - Update Status", new TestCaseDetail { FunctionGroup = "UpdateTopicStatus", TestCaseID = "TC-TOPIC-STS-05", Description = "UpdateAsync and SaveChangesAsync called once", ExpectedResult = "Both Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Persist calls verified" } });
        }

        // TC-TOPIC-STS-06 | A | Repository throws → 500
        [Fact]
        public async Task Handle_RepoThrows_ShouldReturn500()
        {
            var repo = new Mock<ITopicRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB fail"));
            var result = await CreateHandler(repo).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            QACollector.LogTestCase("Topic - Update Status", new TestCaseDetail { FunctionGroup = "UpdateTopicStatus", TestCaseID = "TC-TOPIC-STS-06", Description = "Repository throws → 500", ExpectedResult = "IsSuccess=false, 500", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Exception caught" } });
        }
    }
}