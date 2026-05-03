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
using Tokki.Application.UseCases.Topics.Commands.ApproveTopic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class ApproveTopicCommandHandlerTests
    {
        private static Mock<ITopicRepository> GetRepoMock(Topic? topic = null, int maxOrder = 0)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(topic);
            m.Setup(x => x.GetMaxOrderIndexForVocabAsync()).ReturnsAsync(maxOrder);
            m.Setup(x => x.UpdateAsync(It.IsAny<Topic>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
        }

        private static Mock<IAccountRepository> GetAccountRepoMock(Account? account = null)
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

        private static ApproveTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>?    topicRepo   = null,
            Mock<IAccountRepository>?  accountRepo = null,
            Mock<IEmailService>?       email       = null,
            Mock<IHttpContextAccessor>? http       = null)
            => new ApproveTopicCommandHandler(
                (topicRepo   ?? GetRepoMock()).Object,
                (accountRepo ?? GetAccountRepoMock()).Object,
                (email       ?? GetEmailMock()).Object,
                (http        ?? GetHttpContextMock()).Object,
                NullLogger<ApproveTopicCommandHandler>.Instance);

        private static Topic SampleTopic(TopicStatus status = TopicStatus.PendingApproval) =>
            new Topic { TopicId = "T-001", TopicName = "Korean Grammar", Status = status, CreateBy = "U-001" };

        // ApproveTopic_01 | A | No auth user → 401
        [Fact]
        public async Task Handle_NoAuthUser_ShouldReturn401()
        {
            var result = await CreateHandler(http: GetHttpContextMock(null))
                .Handle(new ApproveTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            QACollector.LogTestCase("Topic - Approve", new TestCaseDetail { FunctionGroup = "ApproveTopic", TestCaseID = "ApproveTopic_01", Description = "No auth user → 401", ExpectedResult = "IsSuccess=false, 401", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No NameIdentifier claim" } });
        }

        // ApproveTopic_02 | A | Topic not found → 404
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(topicRepo: GetRepoMock(null))
                .Handle(new ApproveTopicCommand { TopicId = "MISSING" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Topic - Approve", new TestCaseDetail { FunctionGroup = "ApproveTopic", TestCaseID = "ApproveTopic_02", Description = "Topic not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // ApproveTopic_03 | A | Topic deleted → 400
        [Fact]
        public async Task Handle_TopicDeleted_ShouldReturn400()
        {
            var result = await CreateHandler(topicRepo: GetRepoMock(SampleTopic(TopicStatus.Deleted)))
                .Handle(new ApproveTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Topic - Approve", new TestCaseDetail { FunctionGroup = "ApproveTopic", TestCaseID = "ApproveTopic_03", Description = "Topic deleted → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Status == Deleted" } });
        }

        // ApproveTopic_04 | N | Already Active → idempotent 200
        [Fact]
        public async Task Handle_TopicAlreadyActive_ShouldReturn200Idempotent()
        {
            var result = await CreateHandler(topicRepo: GetRepoMock(SampleTopic(TopicStatus.Active)))
                .Handle(new ApproveTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            QACollector.LogTestCase("Topic - Approve", new TestCaseDetail { FunctionGroup = "ApproveTopic", TestCaseID = "ApproveTopic_04", Description = "Already Active → idempotent 200", ExpectedResult = "IsSuccess=true, 200", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Idempotent: already active" } });
        }

        // ApproveTopic_05 | N | PendingApproval → approved: Status=Active, OrderIndex=maxOrder+1
        [Fact]
        public async Task Handle_PendingApprovalTopic_ShouldSetActiveAndOrderIndex()
        {
            var topic  = SampleTopic(TopicStatus.PendingApproval);
            var repo   = GetRepoMock(topic, maxOrder: 3);
            var result = await CreateHandler(topicRepo: repo)
                .Handle(new ApproveTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            topic.Status.Should().Be(TopicStatus.Active);
            topic.OrderIndex.Should().Be(4);
            QACollector.LogTestCase("Topic - Approve", new TestCaseDetail { FunctionGroup = "ApproveTopic", TestCaseID = "ApproveTopic_05", Description = "PendingApproval → Active, OrderIndex=4", ExpectedResult = "Status=Active, OrderIndex=4", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "maxOrderIndex=3, new=4" } });
        }

        // ApproveTopic_06 | A | Draft topic approve → 400 invalid status
        [Fact]
        public async Task Handle_DraftTopicApprove_ShouldReturn400()
        {
            var result = await CreateHandler(topicRepo: GetRepoMock(SampleTopic(TopicStatus.Draft)))
                .Handle(new ApproveTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Topic - Approve", new TestCaseDetail { FunctionGroup = "ApproveTopic", TestCaseID = "ApproveTopic_06", Description = "Draft approve → 400 (not PendingApproval)", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Only PendingApproval→Active allowed" } });
        }
    }
}