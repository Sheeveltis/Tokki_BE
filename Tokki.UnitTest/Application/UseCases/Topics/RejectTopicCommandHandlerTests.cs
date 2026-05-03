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
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Topics.Commands.RejectTopic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class RejectTopicCommandHandlerTests
    {
        private static Mock<ITopicRepository> GetRepoMock(Topic? topic = null)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(topic);
            m.Setup(x => x.UpdateAsync(It.IsAny<Topic>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
        }

        private static Mock<IAccountRepository> GetAccountMock(Account? account = null)
        {
            var m = new Mock<IAccountRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(account);
            return m;
        }

        private static Mock<IEmailService> GetEmailMock()
        {
            var m = new Mock<IEmailService>();
            m.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            return m;
        }

        private static Mock<IHttpContextAccessor> GetHttpContextMock(string? userId = "ADMIN-01")
        {
            var m    = new Mock<IHttpContextAccessor>();
            var ctx  = new Mock<HttpContext>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
                userId != null ? new[] { new Claim(ClaimTypes.NameIdentifier, userId) } : Array.Empty<Claim>()));
            ctx.Setup(x => x.User).Returns(user);
            m.Setup(x => x.HttpContext).Returns(ctx.Object);
            return m;
        }

        private static RejectTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>?    topicRepo   = null,
            Mock<IAccountRepository>?  accountRepo = null,
            Mock<IEmailService>?       email       = null,
            Mock<IHttpContextAccessor>? http       = null)
            => new RejectTopicCommandHandler(
                (topicRepo   ?? GetRepoMock()).Object,
                (accountRepo ?? GetAccountMock()).Object,
                (email       ?? GetEmailMock()).Object,
                (http        ?? GetHttpContextMock()).Object,
                NullLogger<RejectTopicCommandHandler>.Instance);

        private static Topic SampleTopic(TopicStatus status = TopicStatus.PendingApproval) =>
            new Topic { TopicId = "T-001", TopicName = "Korean Grammar", Status = status, CreateBy = "U-001" };

        private static RejectTopicCommand MakeCmd(string topicId = "T-001", string reason = "Content insufficient") =>
            new RejectTopicCommand { TopicId = topicId, RejectReason = reason };

        // RejectTopic_01 | A | No auth user → 401
        [Fact]
        public async Task Handle_NoAuthUser_ShouldReturn401()
        {
            var result = await CreateHandler(http: GetHttpContextMock(null)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            QACollector.LogTestCase("Topic - Reject", new TestCaseDetail { FunctionGroup = "RejectTopic", TestCaseID = "RejectTopic_01", Description = "No auth user → 401", ExpectedResult = "IsSuccess=false, 401", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No NameIdentifier claim" } });
        }

        // RejectTopic_02 | A | Empty reject reason → 400
        [Fact]
        public async Task Handle_EmptyRejectReason_ShouldReturn400()
        {
            var result = await CreateHandler().Handle(MakeCmd(reason: ""), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Topic - Reject", new TestCaseDetail { FunctionGroup = "RejectTopic", TestCaseID = "RejectTopic_02", Description = "Empty RejectReason → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "RejectReason is required" } });
        }

        // RejectTopic_03 | A | Topic not found → 404
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(topicRepo: GetRepoMock(null)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Topic - Reject", new TestCaseDetail { FunctionGroup = "RejectTopic", TestCaseID = "RejectTopic_03", Description = "Topic not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // RejectTopic_04 | A | Topic not in PendingApproval (Draft) → 400
        [Fact]
        public async Task Handle_TopicNotPendingApproval_ShouldReturn400()
        {
            var result = await CreateHandler(topicRepo: GetRepoMock(SampleTopic(TopicStatus.Draft))).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Topic - Reject", new TestCaseDetail { FunctionGroup = "RejectTopic", TestCaseID = "RejectTopic_04", Description = "Topic not PendingApproval → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Only PendingApproval can be rejected" } });
        }

        // RejectTopic_05 | N | Happy path → Status=Rejected, 200
        [Fact]
        public async Task Handle_ValidRequest_ShouldSetRejectedStatusAndReturn200()
        {
            var topic  = SampleTopic(TopicStatus.PendingApproval);
            var result = await CreateHandler(topicRepo: GetRepoMock(topic)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            topic.Status.Should().Be(TopicStatus.Rejected);
            QACollector.LogTestCase("Topic - Reject", new TestCaseDetail { FunctionGroup = "RejectTopic", TestCaseID = "RejectTopic_05", Description = "Valid request → Status=Rejected, 200", ExpectedResult = "IsSuccess=true, 200, Status=Rejected", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "PendingApproval→Rejected transition" } });
        }

        // RejectTopic_06 | B | UpdateAsync and SaveChangesAsync called on reject
        [Fact]
        public async Task Handle_ValidRequest_UpdateAndSaveCalledOnce()
        {
            var repo = GetRepoMock(SampleTopic(TopicStatus.PendingApproval));
            await CreateHandler(topicRepo: repo).Handle(MakeCmd(), CancellationToken.None);
            repo.Verify(x => x.UpdateAsync(It.IsAny<Topic>()), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Topic - Reject", new TestCaseDetail { FunctionGroup = "RejectTopic", TestCaseID = "RejectTopic_06", Description = "UpdateAsync and SaveChangesAsync called once", ExpectedResult = "Both Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Persist calls verified" } });
        }
    }
}