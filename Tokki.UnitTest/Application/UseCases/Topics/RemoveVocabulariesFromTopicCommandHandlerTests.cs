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
using Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class RemoveVocabulariesFromTopicCommandHandlerTests
    {
        private static Mock<ITopicRepository> GetTopicMock(Topic? topic = null)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(topic);
            return m;
        }

        private static Mock<IVocabularyTopicRepository> GetVtMock(bool success = true, int removed = 1)
        {
            var m = new Mock<IVocabularyTopicRepository>();
            m.Setup(x => x.SoftRemoveVocabulariesFromTopicAsync(
                It.IsAny<string>(), It.IsAny<List<string>>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((success, removed, new List<string>()));
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

        private static Mock<IValidator<RemoveVocabulariesFromTopicCommand>> GetValidatorMock(bool valid = true)
        {
            var m  = new Mock<IValidator<RemoveVocabulariesFromTopicCommand>>();
            var vr = valid ? new ValidationResult() : new ValidationResult(new[] { new ValidationFailure("TopicId", "Required") });
            m.Setup(x => x.ValidateAsync(It.IsAny<RemoveVocabulariesFromTopicCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(vr);
            return m;
        }

        private static RemoveVocabulariesFromTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>?                          topicRepo = null,
            Mock<IVocabularyTopicRepository>?                vtRepo    = null,
            Mock<IHttpContextAccessor>?                      http      = null,
            Mock<IValidator<RemoveVocabulariesFromTopicCommand>>? validator = null)
            => new RemoveVocabulariesFromTopicCommandHandler(
                (topicRepo ?? GetTopicMock(new Topic { TopicId = "T-001", TopicName = "Korean" })).Object,
                (vtRepo    ?? GetVtMock()).Object,
                (http      ?? GetHttpContextMock()).Object,
                (validator ?? GetValidatorMock()).Object);

        private static RemoveVocabulariesFromTopicCommand MakeCmd() =>
            new RemoveVocabulariesFromTopicCommand { TopicId = "T-001", VocabularyIds = new List<string> { "V-001" } };

        // TC-TOPIC-RMVV-01 | A | Validation fails → 400
        [Fact]
        public async Task Handle_ValidationFails_ShouldReturn400()
        {
            var result = await CreateHandler(validator: GetValidatorMock(false)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail { FunctionGroup = "RemoveVocabulariesFromTopic", TestCaseID = "TC-TOPIC-RMVV-01", Description = "Validation fails → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "FluentValidation returns errors" } });
        }

        // TC-TOPIC-RMVV-02 | A | Topic not found → 404
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(topicRepo: GetTopicMock(null)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail { FunctionGroup = "RemoveVocabulariesFromTopic", TestCaseID = "TC-TOPIC-RMVV-02", Description = "Topic not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // TC-TOPIC-RMVV-03 | A | SoftRemove returns success=false → 400
        [Fact]
        public async Task Handle_RemoveFails_ShouldReturn400()
        {
            var result = await CreateHandler(vtRepo: GetVtMock(success: false, removed: 0)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail { FunctionGroup = "RemoveVocabulariesFromTopic", TestCaseID = "TC-TOPIC-RMVV-03", Description = "SoftRemove returns failure → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "SoftRemoveVocabulariesFromTopicAsync returns success=false" } });
        }

        // TC-TOPIC-RMVV-04 | N | SoftRemove success=true, removedCount=0 → 200 (nothing to remove)
        [Fact]
        public async Task Handle_RemovedCountZero_ShouldReturn200()
        {
            var result = await CreateHandler(vtRepo: GetVtMock(success: true, removed: 0)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(0);
            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail { FunctionGroup = "RemoveVocabulariesFromTopic", TestCaseID = "TC-TOPIC-RMVV-04", Description = "success=true, removedCount=0 → 200 with Data=0", ExpectedResult = "IsSuccess=true, 200, Data=0", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Vocab not in topic, nothing removed" } });
        }

        // TC-TOPIC-RMVV-05 | N | Happy path: 2 vocabs removed → 200 with Data=2
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn200WithRemovedCount()
        {
            var result = await CreateHandler(vtRepo: GetVtMock(success: true, removed: 2)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(2);
            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail { FunctionGroup = "RemoveVocabulariesFromTopic", TestCaseID = "TC-TOPIC-RMVV-05", Description = "2 vocabs removed → 200, Data=2", ExpectedResult = "IsSuccess=true, 200, Data=2", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "removedCount=2" } });
        }

        // TC-TOPIC-RMVV-06 | B | SoftRemoveVocabulariesFromTopicAsync called with correct topicId
        [Fact]
        public async Task Handle_ValidRequest_SoftRemoveCalledWithCorrectTopicId()
        {
            var vtRepo = GetVtMock();
            await CreateHandler(vtRepo: vtRepo).Handle(MakeCmd(), CancellationToken.None);
            vtRepo.Verify(x => x.SoftRemoveVocabulariesFromTopicAsync("T-001", It.IsAny<List<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Topic - Remove Vocabularies", new TestCaseDetail { FunctionGroup = "RemoveVocabulariesFromTopic", TestCaseID = "TC-TOPIC-RMVV-06", Description = "SoftRemove called with TopicId='T-001'", ExpectedResult = "Times.Once with correct TopicId", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TopicId forwarded to repo" } });
        }
    }
}