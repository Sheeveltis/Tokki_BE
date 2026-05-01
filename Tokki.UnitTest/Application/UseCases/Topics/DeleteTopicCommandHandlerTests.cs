using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.DeleteTopic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class DeleteTopicCommandHandlerTests
    {
        private static Mock<ITopicRepository> GetRepoMock(Topic? topic = null)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(topic);
            m.Setup(x => x.UpdateAsync(It.IsAny<Topic>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            m.Setup(x => x.DecrementOrderIndexAfterAsync(It.IsAny<int>(), It.IsAny<TopicType>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
            return m;
        }

        private static Mock<IVocabularyTopicRepository> GetVtRepoMock(List<VocabularyTopic>? mappings = null)
        {
            var m = new Mock<IVocabularyTopicRepository>();
            m.Setup(x => x.GetByTopicIdAsync(It.IsAny<string>())).ReturnsAsync(mappings ?? new List<VocabularyTopic>());
            m.Setup(x => x.UpdateAsync(It.IsAny<VocabularyTopic>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1); return m;
        }

        private static Mock<IHttpContextAccessor> GetHttpContextMock(string? userId = "U-001")
        {
            var m    = new Mock<IHttpContextAccessor>();
            var ctx  = new Mock<HttpContext>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
                userId != null ? new[] { new Claim(ClaimTypes.NameIdentifier, userId) } : Array.Empty<Claim>()));
            ctx.Setup(x => x.User).Returns(user);
            m.Setup(x => x.HttpContext).Returns(ctx.Object);
            return m;
        }

        private static DeleteTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>?            topicRepo = null,
            Mock<IVocabularyTopicRepository>?  vtRepo    = null,
            Mock<IHttpContextAccessor>?        http      = null)
            => new DeleteTopicCommandHandler(
                (topicRepo ?? GetRepoMock()).Object,
                (vtRepo    ?? GetVtRepoMock()).Object,
                (http      ?? GetHttpContextMock()).Object,
                NullLogger<DeleteTopicCommandHandler>.Instance);

        private static Topic SampleTopic(TopicStatus status = TopicStatus.Draft, int? orderIndex = 1) =>
            new Topic { TopicId = "T-001", TopicName = "Korean Basics", Status = status, TopicType = TopicType.VocabStudy, OrderIndex = orderIndex };

        // DeleteTopic_01 | A | No auth user → 401
        [Fact]
        public async Task Handle_NoAuthUser_ShouldReturn401()
        {
            var result = await CreateHandler(http: GetHttpContextMock(null))
                .Handle(new DeleteTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            QACollector.LogTestCase("Topic - Delete", new TestCaseDetail { FunctionGroup = "DeleteTopic", TestCaseID = "DeleteTopic_01", Description = "No auth user → 401", ExpectedResult = "IsSuccess=false, 401", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No NameIdentifier claim" } });
        }

        // DeleteTopic_02 | A | Topic not found → 404
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var repo   = GetRepoMock(null);
            var result = await CreateHandler(topicRepo: repo)
                .Handle(new DeleteTopicCommand { TopicId = "MISSING" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Topic - Delete", new TestCaseDetail { FunctionGroup = "DeleteTopic", TestCaseID = "DeleteTopic_02", Description = "Topic not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // DeleteTopic_03 | A | Topic already deleted → 400
        [Fact]
        public async Task Handle_TopicAlreadyDeleted_ShouldReturn400()
        {
            var repo   = GetRepoMock(SampleTopic(TopicStatus.Deleted));
            var result = await CreateHandler(topicRepo: repo)
                .Handle(new DeleteTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Topic - Delete", new TestCaseDetail { FunctionGroup = "DeleteTopic", TestCaseID = "DeleteTopic_03", Description = "Topic already deleted → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Status == Deleted guard" } });
        }

        // DeleteTopic_04 | N | Happy path: topic soft-deleted → Status=Deleted, 200
        [Fact]
        public async Task Handle_ValidRequest_ShouldSoftDeleteTopicAndReturn200()
        {
            var topic    = SampleTopic(TopicStatus.Draft);
            var repo     = GetRepoMock(topic);
            var result   = await CreateHandler(topicRepo: repo)
                .Handle(new DeleteTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            topic.Status.Should().Be(TopicStatus.Deleted);
            QACollector.LogTestCase("Topic - Delete", new TestCaseDetail { FunctionGroup = "DeleteTopic", TestCaseID = "DeleteTopic_04", Description = "Valid request → 200, Status=Deleted", ExpectedResult = "IsSuccess=true, 200, Status=Deleted", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Soft delete applied" } });
        }

        // DeleteTopic_05 | B | UpdateAsync and SaveChangesAsync called on success
        [Fact]
        public async Task Handle_ValidRequest_UpdateAndSaveCalledOnRepo()
        {
            var repo = GetRepoMock(SampleTopic());
            await CreateHandler(topicRepo: repo)
                .Handle(new DeleteTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            repo.Verify(x => x.UpdateAsync(It.IsAny<Topic>()), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Topic - Delete", new TestCaseDetail { FunctionGroup = "DeleteTopic", TestCaseID = "DeleteTopic_05", Description = "UpdateAsync and SaveChangesAsync called once", ExpectedResult = "Both Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Persist calls verified" } });
        }

        // DeleteTopic_06 | A | Repository throws → 500 failure
        [Fact]
        public async Task Handle_RepoThrows_ShouldReturn500()
        {
            var repo = new Mock<ITopicRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB error"));
            var result = await CreateHandler(topicRepo: repo)
                .Handle(new DeleteTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            QACollector.LogTestCase("Topic - Delete", new TestCaseDetail { FunctionGroup = "DeleteTopic", TestCaseID = "DeleteTopic_06", Description = "Repository throws → 500", ExpectedResult = "IsSuccess=false, 500", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Exception caught in try-catch" } });
        }

        // DeleteTopic_07 | N | Null OrderIndex should not execute DecrementOrderIndexAfterAsync
        [Fact]
        public async Task Handle_NullOrderIndex_ShouldSoftDeleteWithoutDecrementing()
        {
            var topic = SampleTopic(TopicStatus.Draft, null); // OrderIndex = null
            var repo  = GetRepoMock(topic);
            
            var result = await CreateHandler(topicRepo: repo)
                .Handle(new DeleteTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            
            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.DecrementOrderIndexAfterAsync(It.IsAny<int>(), It.IsAny<TopicType>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
            
            QACollector.LogTestCase("Topic - Delete", new TestCaseDetail { FunctionGroup = "DeleteTopic", TestCaseID = "DeleteTopic_07", Description = "OrderIndex is null so DecrementOrderIndexAfterAsync is completely avoided", ExpectedResult = "Success without Decrement call", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "OrderIndex = null guard fails safely" } });
        }

        // DeleteTopic_08 | N | Vocabulary mappings exist, iterates and updates them
        [Fact]
        public async Task Handle_MappingsExist_ShouldSoftDeleteMappings()
        {
            var topic = SampleTopic(TopicStatus.Draft);
            var repo  = GetRepoMock(topic);
            
            var mappings = new List<VocabularyTopic>
            {
                new VocabularyTopic { TopicId = "T-001", VocabularyId = "V-1", Status = VocabularyTopicStatus.Active },
                new VocabularyTopic { TopicId = "T-001", VocabularyId = "V-2", Status = VocabularyTopicStatus.Active }
            };
            var vtRepo = GetVtRepoMock(mappings);
            
            var result = await CreateHandler(topicRepo: repo, vtRepo: vtRepo)
                .Handle(new DeleteTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            
            result.IsSuccess.Should().BeTrue();
            // Expect update called 2 times for 2 mappings
            vtRepo.Verify(x => x.UpdateAsync(It.IsAny<VocabularyTopic>()), Times.Exactly(2));
            
            QACollector.LogTestCase("Topic - Delete", new TestCaseDetail { FunctionGroup = "DeleteTopic", TestCaseID = "DeleteTopic_08", Description = "Loop over existing topic mappings and update child statuses", ExpectedResult = "Success, UpdateAsync for Mappings called correctly", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "mappings.Count > 0" } });
        }
    }
}