using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class UpdateTopicCommandHandlerTests
    {
        private static Mock<ITopicRepository> GetRepoMock(Topic? topic = null, bool nameExists = false)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(topic);
            m.Setup(x => x.IsTopicNameExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(nameExists);
            m.Setup(x => x.UpdateAsync(It.IsAny<Topic>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
        }

        private static Mock<IVocabularyTopicRepository> GetVtRepoMock()
        {
            var m = new Mock<IVocabularyTopicRepository>();
            m.Setup(x => x.GetByTopicIdAsync(It.IsAny<string>())).ReturnsAsync(new List<VocabularyTopic>());
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1); return m;
        }

        private static UpdateTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>?           topicRepo = null,
            Mock<IVocabularyTopicRepository>? vtRepo    = null)
            => new UpdateTopicCommandHandler(
                (topicRepo ?? GetRepoMock()).Object,
                (vtRepo    ?? GetVtRepoMock()).Object,
                NullLogger<UpdateTopicCommandHandler>.Instance);

        private static Topic SampleTopic(TopicStatus status = TopicStatus.Draft) =>
            new Topic { TopicId = "T-001", TopicName = "Old Name", Description = "Old desc", Status = status, Level = TopicLevel.Level1 };

        private static UpdateTopicCommand MakeCommand(string id = "T-001", string? name = "New Name") =>
            new UpdateTopicCommand { TopicId = id, TopicName = name, Description = "New desc", UpdatedBy = "U-001" };

        // TC-TOPIC-UPD-01 | A | Topic not found → 404
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(GetRepoMock(null)).Handle(MakeCommand("MISSING"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Topic - Update", new TestCaseDetail { FunctionGroup = "UpdateTopic", TestCaseID = "TC-TOPIC-UPD-01", Description = "Topic not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // TC-TOPIC-UPD-02 | A | Topic already deleted → 409
        [Fact]
        public async Task Handle_TopicAlreadyDeleted_ShouldReturn409()
        {
            var result = await CreateHandler(GetRepoMock(SampleTopic(TopicStatus.Deleted))).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            QACollector.LogTestCase("Topic - Update", new TestCaseDetail { FunctionGroup = "UpdateTopic", TestCaseID = "TC-TOPIC-UPD-02", Description = "Topic already deleted → 409", ExpectedResult = "IsSuccess=false, 409", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Status == Deleted guard" } });
        }

        // TC-TOPIC-UPD-03 | A | New name duplicates another topic → 409
        [Fact]
        public async Task Handle_DuplicateTopicName_ShouldReturn409()
        {
            var repo   = GetRepoMock(SampleTopic(), nameExists: true);
            var result = await CreateHandler(repo).Handle(MakeCommand(name: "Duplicate Name"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            QACollector.LogTestCase("Topic - Update", new TestCaseDetail { FunctionGroup = "UpdateTopic", TestCaseID = "TC-TOPIC-UPD-03", Description = "Duplicate name → 409", ExpectedResult = "IsSuccess=false, 409", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "IsTopicNameExistsAsync returns true" } });
        }

        // TC-TOPIC-UPD-04 | N | Happy path: topic found, valid update → 200 true
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn200True()
        {
            var repo   = GetRepoMock(SampleTopic());
            var result = await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeTrue();
            QACollector.LogTestCase("Topic - Update", new TestCaseDetail { FunctionGroup = "UpdateTopic", TestCaseID = "TC-TOPIC-UPD-04", Description = "Valid update → 200, Data=true", ExpectedResult = "IsSuccess=true, 200, Data=true", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "All guards pass" } });
        }

        // TC-TOPIC-UPD-05 | N | TopicName null → topic name not updated on entity
        [Fact]
        public async Task Handle_NullTopicName_TopicNameNotUpdated()
        {
            var topic = SampleTopic();
            var repo  = GetRepoMock(topic);
            await CreateHandler(repo).Handle(MakeCommand(name: null), CancellationToken.None);
            topic.TopicName.Should().Be("Old Name"); // unchanged
            QACollector.LogTestCase("Topic - Update", new TestCaseDetail { FunctionGroup = "UpdateTopic", TestCaseID = "TC-TOPIC-UPD-05", Description = "TopicName null → entity name not updated", ExpectedResult = "TopicName still 'Old Name'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Null TopicName skips name update" } });
        }

        // TC-TOPIC-UPD-06 | A | Repository throws → 500 failure
        [Fact]
        public async Task Handle_RepoThrows_ShouldReturn500()
        {
            var repo = new Mock<ITopicRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB fail"));
            var result = await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            QACollector.LogTestCase("Topic - Update", new TestCaseDetail { FunctionGroup = "UpdateTopic", TestCaseID = "TC-TOPIC-UPD-06", Description = "Repository throws → 500", ExpectedResult = "IsSuccess=false, 500", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Exception caught" } });
        }

        // TC-TOPIC-UPD-07 | N | Update status to Deleted triggers VocabularyTopic cascade
        [Fact]
        public async Task Handle_StatusChangedToDeleted_ShouldCascadeSoftDeleteToMappings()
        {
            var topic = SampleTopic(TopicStatus.Draft);
            var repo = GetRepoMock(topic);
            
            var mappings = new List<VocabularyTopic> { new VocabularyTopic { TopicId = "T-001", Status = VocabularyTopicStatus.Active } };
            var vtRepo = new Mock<IVocabularyTopicRepository>();
            vtRepo.Setup(x => x.GetByTopicIdAsync(It.IsAny<string>())).ReturnsAsync(mappings);
            
            var handler = CreateHandler(repo, vtRepo);
            var cmd = MakeCommand();
            cmd.Status = TopicStatus.Deleted; // Update status to deleted
            
            var result = await handler.Handle(cmd, CancellationToken.None);
            
            result.IsSuccess.Should().BeTrue();
            // Verify cascade update on mapping was called once
            vtRepo.Verify(x => x.UpdateAsync(It.IsAny<VocabularyTopic>()), Times.Once);
            vtRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Topic - Update", new TestCaseDetail { FunctionGroup = "UpdateTopic", TestCaseID = "TC-TOPIC-UPD-07", Description = "Status changed to Deleted triggers cascade soft delete", ExpectedResult = "IsSuccess=true, UpdateAsync called on vtRepo", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "newStatus == TopicStatus.Deleted cascade branch" } });
        }
    }
}