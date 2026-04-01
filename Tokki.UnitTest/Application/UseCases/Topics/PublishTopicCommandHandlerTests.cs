using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.PublishTopic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class PublishTopicCommandHandlerTests
    {
        private static Mock<ITopicRepository> GetRepoMock(Topic? topic = null, int maxOrderIndex = 0)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(topic);
            m.Setup(x => x.GetMaxOrderIndexAsync()).ReturnsAsync(maxOrderIndex);
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

        private static Mock<IValidator<PublishTopicCommand>> GetValidatorMock(bool valid = true)
        {
            var m  = new Mock<IValidator<PublishTopicCommand>>();
            var vr = valid ? new ValidationResult() : new ValidationResult(new[] { new ValidationFailure("TopicId", "Required") });
            m.Setup(x => x.ValidateAsync(It.IsAny<PublishTopicCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(vr);
            return m;
        }

        private static PublishTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>?              repo      = null,
            Mock<IHttpContextAccessor>?          http      = null,
            Mock<IValidator<PublishTopicCommand>>? validator = null)
            => new PublishTopicCommandHandler(
                (repo      ?? GetRepoMock()).Object,
                (http      ?? GetHttpContextMock()).Object,
                (validator ?? GetValidatorMock()).Object);

        private static Topic SampleTopic(TopicStatus status = TopicStatus.Draft) =>
            new Topic { TopicId = "T-001", TopicName = "Korean Basics", Status = status };

        // TC-TOPIC-PUB-01 | A | No auth user → 401
        [Fact]
        public async Task Handle_NoAuthUser_ShouldReturn401()
        {
            var result = await CreateHandler(http: GetHttpContextMock(null))
                .Handle(new PublishTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            QACollector.LogTestCase("Topic - Publish", new TestCaseDetail { FunctionGroup = "PublishTopic", TestCaseID = "TC-TOPIC-PUB-01", Description = "No auth user → 401", ExpectedResult = "IsSuccess=false, 401", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No NameIdentifier claim" } });
        }

        // TC-TOPIC-PUB-02 | A | Topic not found → 404
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(repo: GetRepoMock(null))
                .Handle(new PublishTopicCommand { TopicId = "MISSING" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Topic - Publish", new TestCaseDetail { FunctionGroup = "PublishTopic", TestCaseID = "TC-TOPIC-PUB-02", Description = "Topic not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // TC-TOPIC-PUB-03 | A | Topic already deleted → 400
        [Fact]
        public async Task Handle_TopicDeleted_ShouldReturn400()
        {
            var result = await CreateHandler(repo: GetRepoMock(SampleTopic(TopicStatus.Deleted)))
                .Handle(new PublishTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Topic - Publish", new TestCaseDetail { FunctionGroup = "PublishTopic", TestCaseID = "TC-TOPIC-PUB-03", Description = "Topic deleted → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Status == Deleted" } });
        }

        // TC-TOPIC-PUB-04 | N | Topic already Active → idempotent 200
        [Fact]
        public async Task Handle_TopicAlreadyActive_ShouldReturn200Idempotent()
        {
            var result = await CreateHandler(repo: GetRepoMock(SampleTopic(TopicStatus.Active)))
                .Handle(new PublishTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            QACollector.LogTestCase("Topic - Publish", new TestCaseDetail { FunctionGroup = "PublishTopic", TestCaseID = "TC-TOPIC-PUB-04", Description = "Already Active → idempotent 200", ExpectedResult = "IsSuccess=true, 200", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Idempotent: already active" } });
        }

        // TC-TOPIC-PUB-05 | N | Draft topic → published: Status=Active, OrderIndex=maxOrderIndex+1
        [Fact]
        public async Task Handle_DraftTopic_ShouldPublishWithCorrectStatus()
        {
            var topic  = SampleTopic(TopicStatus.Draft);
            var repo   = GetRepoMock(topic, maxOrderIndex: 5);
            var result = await CreateHandler(repo: repo)
                .Handle(new PublishTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            topic.Status.Should().Be(TopicStatus.Active);
            topic.OrderIndex.Should().Be(6);
            QACollector.LogTestCase("Topic - Publish", new TestCaseDetail { FunctionGroup = "PublishTopic", TestCaseID = "TC-TOPIC-PUB-05", Description = "Draft → Active, OrderIndex=6 (maxOrderIndex+1)", ExpectedResult = "Status=Active, OrderIndex=6", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "maxOrderIndex=5, new=6" } });
        }

        // TC-TOPIC-PUB-06 | A | Non-Draft topic (e.g. PendingApproval) → 400 invalid transition
        [Fact]
        public async Task Handle_PendingApprovalTopic_ShouldReturn400InvalidTransition()
        {
            var result = await CreateHandler(repo: GetRepoMock(SampleTopic(TopicStatus.PendingApproval)))
                .Handle(new PublishTopicCommand { TopicId = "T-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Topic - Publish", new TestCaseDetail { FunctionGroup = "PublishTopic", TestCaseID = "TC-TOPIC-PUB-06", Description = "PendingApproval → 400 invalid status transition", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Only Draft→Active allowed" } });
        }
    }
}