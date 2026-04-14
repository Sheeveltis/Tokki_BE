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
using Tokki.Application.UseCases.Topics.Commands.CreateTopic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class CreateTopicCommandHandlerTests
    {
        private static Mock<ITopicRepository> GetRepoMock(bool nameExists = false)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.IsTopicNameExistsAsync(It.IsAny<string>(), It.IsAny<string?>()))
             .ReturnsAsync(nameExists);
            m.Setup(x => x.AddAsync(It.IsAny<Topic>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
        }

        private static Mock<IIdGeneratorService> GetIdGenMock(string id = "TP-001")
        {
            var m = new Mock<IIdGeneratorService>();
            m.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns(id);
            return m;
        }

        private static Mock<IHttpContextAccessor> GetHttpContextMock(string? userId = "U-001")
        {
            var m       = new Mock<IHttpContextAccessor>();
            var ctx     = new Mock<HttpContext>();
            var user    = new ClaimsPrincipal(new ClaimsIdentity(
                userId != null ? new[] { new Claim(ClaimTypes.NameIdentifier, userId) } : Array.Empty<Claim>()));
            ctx.Setup(x => x.User).Returns(user);
            m.Setup(x => x.HttpContext).Returns(ctx.Object);
            return m;
        }

        private static CreateTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>?    repo    = null,
            Mock<IIdGeneratorService>? idGen   = null,
            Mock<IHttpContextAccessor>? http   = null)
            => new CreateTopicCommandHandler(
                (repo  ?? GetRepoMock()).Object,
                (idGen ?? GetIdGenMock()).Object,
                NullLogger<CreateTopicCommandHandler>.Instance,
                (http  ?? GetHttpContextMock()).Object);

        private static CreateTopicCommand MakeCommand(string name = "Korean Basics")
            => new CreateTopicCommand { TopicName = name, Level = TopicLevel.Level1, Description = "Test topic" };

        // TC-TOPIC-CREATE-01 | A | No authenticated user → 401 failure
        [Fact]
        public async Task Handle_NoAuthUser_ShouldReturn401()
        {
            var result = await CreateHandler(http: GetHttpContextMock(null)).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            QACollector.LogTestCase("Topic - Create", new TestCaseDetail { FunctionGroup = "CreateTopic", TestCaseID = "TC-TOPIC-CREATE-01", Description = "No auth user → 401", ExpectedResult = "IsSuccess=false, 401", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "HttpContext User has no NameIdentifier claim" } });
        }

        // TC-TOPIC-CREATE-02 | A | Duplicate topic name → 409 conflict
        [Fact]
        public async Task Handle_DuplicateTopicName_ShouldReturn409()
        {
            var repo   = GetRepoMock(nameExists: true);
            var result = await CreateHandler(repo: repo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            QACollector.LogTestCase("Topic - Create", new TestCaseDetail { FunctionGroup = "CreateTopic", TestCaseID = "TC-TOPIC-CREATE-02", Description = "Duplicate topic name → 409", ExpectedResult = "IsSuccess=false, 409", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "IsTopicNameExistsAsync returns true" } });
        }

        // TC-TOPIC-CREATE-03 | N | Happy path → 201 with generated TopicId
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn201WithTopicId()
        {
            var idGen  = GetIdGenMock("TP-NEW");
            var result = await CreateHandler(idGen: idGen).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("TP-NEW");
            QACollector.LogTestCase("Topic - Create", new TestCaseDetail { FunctionGroup = "CreateTopic", TestCaseID = "TC-TOPIC-CREATE-03", Description = "Valid request → 201, Data='TP-NEW'", ExpectedResult = "IsSuccess=true, 201", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Auth OK, no duplicate" } });
        }

        // TC-TOPIC-CREATE-04 | N | Topic created with Status=Draft and TopicType=VocabStudy
        [Fact]
        public async Task Handle_ValidRequest_TopicCreatedWithDraftStatusAndVocabStudyType()
        {
            Topic? captured = null;
            var repo = GetRepoMock();
            repo.Setup(x => x.AddAsync(It.IsAny<Topic>())).Callback<Topic>(t => captured = t).Returns(Task.CompletedTask);
            await CreateHandler(repo: repo).Handle(MakeCommand(), CancellationToken.None);
            captured.Should().NotBeNull();
            captured!.Status.Should().Be(TopicStatus.Draft);
            captured.TopicType.Should().Be(TopicType.VocabStudy);
            QACollector.LogTestCase("Topic - Create", new TestCaseDetail { FunctionGroup = "CreateTopic", TestCaseID = "TC-TOPIC-CREATE-04", Description = "New topic: Status=Draft, TopicType=VocabStudy", ExpectedResult = "Status=Draft, TopicType=VocabStudy", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Status defaults to Draft" } });
        }

        // TC-TOPIC-CREATE-05 | B | AddAsync and SaveChangesAsync both called once
        [Fact]
        public async Task Handle_ValidRequest_AddAndSaveBothCalledOnce()
        {
            var repo = GetRepoMock();
            await CreateHandler(repo: repo).Handle(MakeCommand(), CancellationToken.None);
            repo.Verify(x => x.AddAsync(It.IsAny<Topic>()), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Topic - Create", new TestCaseDetail { FunctionGroup = "CreateTopic", TestCaseID = "TC-TOPIC-CREATE-05", Description = "AddAsync and SaveChangesAsync both called once", ExpectedResult = "Both Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Persist calls verified" } });
        }

        // TC-TOPIC-CREATE-06 | A | Repository throws → 500 failure
        [Fact]
        public async Task Handle_RepoThrows_ShouldReturn500()
        {
            var repo = new Mock<ITopicRepository>();
            repo.Setup(x => x.IsTopicNameExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ThrowsAsync(new Exception("DB fail"));
            var result = await CreateHandler(repo: repo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            QACollector.LogTestCase("Topic - Create", new TestCaseDetail { FunctionGroup = "CreateTopic", TestCaseID = "TC-TOPIC-CREATE-06", Description = "Repository throws → 500", ExpectedResult = "IsSuccess=false, 500", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Exception caught" } });
        }
    }
}