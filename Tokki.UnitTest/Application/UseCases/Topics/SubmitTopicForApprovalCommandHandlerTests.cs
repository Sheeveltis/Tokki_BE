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
using Tokki.Application.UseCases.Topics.Commands.SubmitTopicForApproval;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class SubmitTopicForApprovalCommandHandlerTests
    {
        private static Mock<ITopicRepository> GetRepoMock(Topic? topic = null)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(topic);
            m.Setup(x => x.UpdateAsync(It.IsAny<Topic>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
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

        private static SubmitTopicForApprovalCommandHandler CreateHandler(
            Mock<ITopicRepository>?    repo = null,
            Mock<IHttpContextAccessor>? http = null)
            => new SubmitTopicForApprovalCommandHandler(
                (repo ?? GetRepoMock()).Object,
                (http ?? GetHttpContextMock()).Object,
                NullLogger<SubmitTopicForApprovalCommandHandler>.Instance);

        private static Topic SampleTopic(TopicStatus status = TopicStatus.Draft) =>
            new Topic { TopicId = "T-001", TopicName = "Korean Grammar", Status = status };

        // SubmitTopicForApproval_01 | A | No auth user → 401
        [Fact]
        public async Task Handle_NoAuthUser_ShouldReturn401()
        {
            var result = await CreateHandler(http: GetHttpContextMock(null))
                .Handle(new SubmitTopicForApprovalCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            QACollector.LogTestCase("Topic - Submit For Approval", new TestCaseDetail { FunctionGroup = "SubmitTopicForApproval", TestCaseID = "SubmitTopicForApproval_01", Description = "No auth user → 401", ExpectedResult = "IsSuccess=false, 401", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No claim" } });
        }

        // SubmitTopicForApproval_02 | A | Topic not found → 404
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(repo: GetRepoMock(null))
                .Handle(new SubmitTopicForApprovalCommand { TopicId = "MISSING" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Topic - Submit For Approval", new TestCaseDetail { FunctionGroup = "SubmitTopicForApproval", TestCaseID = "SubmitTopicForApproval_02", Description = "Topic not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // SubmitTopicForApproval_03 | A | Non-Draft topic → 400
        [Fact]
        public async Task Handle_TopicNotDraft_ShouldReturn400()
        {
            var result = await CreateHandler(repo: GetRepoMock(SampleTopic(TopicStatus.Active)))
                .Handle(new SubmitTopicForApprovalCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Topic - Submit For Approval", new TestCaseDetail { FunctionGroup = "SubmitTopicForApproval", TestCaseID = "SubmitTopicForApproval_03", Description = "Non-Draft submit → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Only Draft can be submitted" } });
        }

        // SubmitTopicForApproval_04 | N | Draft → PendingApproval, 200
        [Fact]
        public async Task Handle_DraftTopic_ShouldSetPendingApprovalAndReturn200()
        {
            var topic  = SampleTopic(TopicStatus.Draft);
            var result = await CreateHandler(repo: GetRepoMock(topic))
                .Handle(new SubmitTopicForApprovalCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            topic.Status.Should().Be(TopicStatus.PendingApproval);
            QACollector.LogTestCase("Topic - Submit For Approval", new TestCaseDetail { FunctionGroup = "SubmitTopicForApproval", TestCaseID = "SubmitTopicForApproval_04", Description = "Draft → PendingApproval, 200", ExpectedResult = "IsSuccess=true, 200, Status=PendingApproval", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Draft→PendingApproval" } });
        }

        // SubmitTopicForApproval_05 | B | UpdateAsync and SaveChangesAsync called once
        [Fact]
        public async Task Handle_ValidRequest_UpdateAndSaveCalledOnce()
        {
            var repo = GetRepoMock(SampleTopic(TopicStatus.Draft));
            await CreateHandler(repo: repo).Handle(new SubmitTopicForApprovalCommand { TopicId = "T-001" }, CancellationToken.None);
            repo.Verify(x => x.UpdateAsync(It.IsAny<Topic>()), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Topic - Submit For Approval", new TestCaseDetail { FunctionGroup = "SubmitTopicForApproval", TestCaseID = "SubmitTopicForApproval_05", Description = "UpdateAsync and SaveChangesAsync called once", ExpectedResult = "Both Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Persist calls verified" } });
        }

        // SubmitTopicForApproval_06 | A | Already PendingApproval → 400
        [Fact]
        public async Task Handle_AlreadyPendingApproval_ShouldReturn400()
        {
            var result = await CreateHandler(repo: GetRepoMock(SampleTopic(TopicStatus.PendingApproval)))
                .Handle(new SubmitTopicForApprovalCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Topic - Submit For Approval", new TestCaseDetail { FunctionGroup = "SubmitTopicForApproval", TestCaseID = "SubmitTopicForApproval_06", Description = "Already PendingApproval → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Not Draft status" } });
        }
    }
}