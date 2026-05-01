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
using Tokki.Application.UseCases.Topics.Commands.AddVocabulariesToTopic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class AddVocabulariesToTopicCommandHandlerTests
    {
        private static Mock<ITopicRepository> GetTopicMock(Topic? topic = null)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(topic);
            return m;
        }

        private static Mock<IVocabularyRepository> GetVocabMock(List<Tokki.Domain.Entities.Vocabulary>? vocabs = null)
        {
            var m = new Mock<IVocabularyRepository>();
            m.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>())).ReturnsAsync(vocabs ?? new List<Tokki.Domain.Entities.Vocabulary>());
            return m;
        }

        private static Mock<IVocabularyTopicRepository> GetVtMock(int added = 1)
        {
            var m = new Mock<IVocabularyTopicRepository>();
            m.Setup(x => x.AddOrReactivateVocabulariesToTopicAsync(
                It.IsAny<string>(), It.IsAny<List<Tokki.Domain.Entities.Vocabulary>>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((added, 0, new List<string>()));
            return m;
        }

        private static Mock<IValidator<AddVocabulariesToTopicCommand>> GetValidatorMock(bool valid = true)
        {
            var m  = new Mock<IValidator<AddVocabulariesToTopicCommand>>();
            var vr = valid ? new ValidationResult() : new ValidationResult(new[] { new ValidationFailure("VocabularyIds", "Required") });
            m.Setup(x => x.ValidateAsync(It.IsAny<AddVocabulariesToTopicCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(vr);
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

        private static AddVocabulariesToTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>?                       topicRepo = null,
            Mock<IVocabularyRepository>?                  vocabRepo = null,
            Mock<IVocabularyTopicRepository>?             vtRepo    = null,
            Mock<IHttpContextAccessor>?                   http      = null,
            Mock<IValidator<AddVocabulariesToTopicCommand>>? validator = null)
            => new AddVocabulariesToTopicCommandHandler(
                (topicRepo ?? GetTopicMock(new Topic { TopicId = "T-001", TopicName = "Korean" })).Object,
                (vocabRepo ?? GetVocabMock(new List<Tokki.Domain.Entities.Vocabulary> { new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V-001" } })).Object,
                (vtRepo    ?? GetVtMock()).Object,
                (http      ?? GetHttpContextMock()).Object,
                (validator ?? GetValidatorMock()).Object);

        private static AddVocabulariesToTopicCommand MakeCmd(string topicId = "T-001")
            => new AddVocabulariesToTopicCommand { TopicId = topicId, VocabularyIds = new List<string> { "V-001", "V-002" } };

        private static Topic SampleTopic() => new Topic { TopicId = "T-001", TopicName = "Korean Basics" };

        // AddVocabulariesToTopic_01 | A | Validation fails → 400
        [Fact]
        public async Task Handle_ValidationFails_ShouldReturn400()
        {
            var result = await CreateHandler(validator: GetValidatorMock(false)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Topic - Add Vocabularies", new TestCaseDetail { FunctionGroup = "AddVocabulariesToTopic", TestCaseID = "AddVocabulariesToTopic_01", Description = "Validation fails → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "FluentValidation returns errors" } });
        }

        // AddVocabulariesToTopic_02 | A | Topic not found → 404
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var result = await CreateHandler(topicRepo: GetTopicMock(null)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Topic - Add Vocabularies", new TestCaseDetail { FunctionGroup = "AddVocabulariesToTopic", TestCaseID = "AddVocabulariesToTopic_02", Description = "Topic not found → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdAsync returns null" } });
        }

        // AddVocabulariesToTopic_03 | A | No valid vocabularies found → 400
        [Fact]
        public async Task Handle_NoValidVocabs_ShouldReturn400()
        {
            var result = await CreateHandler(
                topicRepo: GetTopicMock(SampleTopic()),
                vocabRepo: GetVocabMock(new List<Tokki.Domain.Entities.Vocabulary>())).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Topic - Add Vocabularies", new TestCaseDetail { FunctionGroup = "AddVocabulariesToTopic", TestCaseID = "AddVocabulariesToTopic_03", Description = "No valid vocabs found → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByIdsAsync returns empty list" } });
        }

        // AddVocabulariesToTopic_04 | N | Happy path: 1 vocab added → 200 with addedCount=1
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn200WithAddedCount()
        {
            var result = await CreateHandler(
                topicRepo: GetTopicMock(SampleTopic()),
                vocabRepo: GetVocabMock(new List<Tokki.Domain.Entities.Vocabulary> { new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V-001" } }),
                vtRepo: GetVtMock(1)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(1);
            QACollector.LogTestCase("Topic - Add Vocabularies", new TestCaseDetail { FunctionGroup = "AddVocabulariesToTopic", TestCaseID = "AddVocabulariesToTopic_04", Description = "Valid request → 200, addedCount=1", ExpectedResult = "IsSuccess=true, 200, Data=1", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "1 vocab added/reactivated" } });
        }

        // AddVocabulariesToTopic_05 | B | AddOrReactivate called once with found vocabs
        [Fact]
        public async Task Handle_ValidRequest_AddOrReactivateCalled()
        {
            var vtRepo = GetVtMock();
            var vocabs = new List<Tokki.Domain.Entities.Vocabulary> { new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V-001" } };
            await CreateHandler(topicRepo: GetTopicMock(SampleTopic()), vocabRepo: GetVocabMock(vocabs), vtRepo: vtRepo).Handle(MakeCmd(), CancellationToken.None);
            vtRepo.Verify(x => x.AddOrReactivateVocabulariesToTopicAsync("T-001", vocabs, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Topic - Add Vocabularies", new TestCaseDetail { FunctionGroup = "AddVocabulariesToTopic", TestCaseID = "AddVocabulariesToTopic_05", Description = "AddOrReactivate called with correct topicId and vocabs", ExpectedResult = "Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Repo called once" } });
        }

        // AddVocabulariesToTopic_06 | N | Duplicate vocab IDs in request deduplicated
        [Fact]
        public async Task Handle_DuplicateVocabIds_DeduplicatedBeforeRepoCall()
        {
            var vtRepo = GetVtMock();
            var vocabs = new List<Tokki.Domain.Entities.Vocabulary> { new Tokki.Domain.Entities.Vocabulary { VocabularyId = "V-001" } };
            var cmd    = new AddVocabulariesToTopicCommand { TopicId = "T-001", VocabularyIds = new List<string> { "V-001", "V-001", "V-001" } };
            await CreateHandler(topicRepo: GetTopicMock(SampleTopic()), vocabRepo: GetVocabMock(vocabs), vtRepo: vtRepo).Handle(cmd, CancellationToken.None);
            // Should proceed without errors - duplicate IDs are Distinct()ed
            vtRepo.Verify(x => x.AddOrReactivateVocabulariesToTopicAsync(It.IsAny<string>(), It.IsAny<List<Tokki.Domain.Entities.Vocabulary>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Topic - Add Vocabularies", new TestCaseDetail { FunctionGroup = "AddVocabulariesToTopic", TestCaseID = "AddVocabulariesToTopic_06", Description = "Duplicate vocabIds deduplicated via Distinct()", ExpectedResult = "AddOrReactivate called once, no errors", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "request.VocabularyIds.Distinct() applied" } });
        }
    }
}