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
using Tokki.Application.UseCases.Topics.Commands.CreateTopicByStaff;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class CreateTopicByStaffCommandHandlerTests
    {
        private static Mock<ITopicRepository> GetRepoMock(bool nameExists = false)
        {
            var m = new Mock<ITopicRepository>();
            m.Setup(x => x.IsTopicNameExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(nameExists);
            m.Setup(x => x.AddAsync(It.IsAny<Topic>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return m;
        }

        private static Mock<IIdGeneratorService> GetIdGenMock(string id = "TP-STAFF-001")
        {
            var m = new Mock<IIdGeneratorService>();
            m.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns(id);
            return m;
        }

        private static Mock<IHttpContextAccessor> GetHttpContextMock(string? userId = "STAFF-01")
        {
            var m    = new Mock<IHttpContextAccessor>();
            var ctx  = new Mock<HttpContext>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(
                userId != null ? new[] { new Claim(ClaimTypes.NameIdentifier, userId) } : Array.Empty<Claim>()));
            ctx.Setup(x => x.User).Returns(user);
            m.Setup(x => x.HttpContext).Returns(ctx.Object);
            return m;
        }

        private static CreateTopicByStaffCommandHandler CreateHandler(
            Mock<ITopicRepository>?      repo  = null,
            Mock<IIdGeneratorService>?   idGen = null,
            Mock<IHttpContextAccessor>?  http  = null)
            => new CreateTopicByStaffCommandHandler(
                (repo  ?? GetRepoMock()).Object,
                (idGen ?? GetIdGenMock()).Object,
                (http  ?? GetHttpContextMock()).Object,
                NullLogger<CreateTopicByStaffCommandHandler>.Instance);

        private static CreateTopicByStaffCommand MakeCmd(string name = "Korean Grammar") =>
            new CreateTopicByStaffCommand { TopicName = name, Level = (int)TopicLevel.Level1 };

        // CreateTopicByStaff_01 | A | No auth user → 401
        [Fact]
        public async Task Handle_NoAuthUser_ShouldReturn401()
        {
            var result = await CreateHandler(http: GetHttpContextMock(null)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            QACollector.LogTestCase("Topic - Create By Staff", new TestCaseDetail { FunctionGroup = "CreateTopicByStaff", TestCaseID = "CreateTopicByStaff_01", Description = "No auth user → 401", ExpectedResult = "IsSuccess=false, 401", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No NameIdentifier claim" } });
        }

        // CreateTopicByStaff_02 | A | Duplicate topic name → 409
        [Fact]
        public async Task Handle_DuplicateName_ShouldReturn409()
        {
            var result = await CreateHandler(repo: GetRepoMock(nameExists: true)).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            QACollector.LogTestCase("Topic - Create By Staff", new TestCaseDetail { FunctionGroup = "CreateTopicByStaff", TestCaseID = "CreateTopicByStaff_02", Description = "Duplicate name → 409", ExpectedResult = "IsSuccess=false, 409", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "IsTopicNameExistsAsync returns true" } });
        }

        // CreateTopicByStaff_03 | N | Happy path → 201 with TopicId
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn201WithTopicId()
        {
            var result = await CreateHandler(idGen: GetIdGenMock("TP-STAFF-NEW")).Handle(MakeCmd(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("TP-STAFF-NEW");
            QACollector.LogTestCase("Topic - Create By Staff", new TestCaseDetail { FunctionGroup = "CreateTopicByStaff", TestCaseID = "CreateTopicByStaff_03", Description = "Valid request → 201, TopicId='TP-STAFF-NEW'", ExpectedResult = "IsSuccess=true, 201", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Auth OK, no duplicate" } });
        }

        // CreateTopicByStaff_04 | N | Topic created with Status=PendingApproval
        [Fact]
        public async Task Handle_ValidRequest_TopicCreatedWithPendingApprovalStatus()
        {
            Topic? captured = null;
            var repo = GetRepoMock();
            repo.Setup(x => x.AddAsync(It.IsAny<Topic>())).Callback<Topic>(t => captured = t).Returns(Task.CompletedTask);
            await CreateHandler(repo: repo).Handle(MakeCmd(), CancellationToken.None);
            captured.Should().NotBeNull();
            captured!.Status.Should().Be(TopicStatus.PendingApproval);
            captured.TopicType.Should().Be(TopicType.VocabStudy);
            QACollector.LogTestCase("Topic - Create By Staff", new TestCaseDetail { FunctionGroup = "CreateTopicByStaff", TestCaseID = "CreateTopicByStaff_04", Description = "Topic has Status=PendingApproval (awaiting admin approval)", ExpectedResult = "Status=PendingApproval, TopicType=VocabStudy", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Staff topics require approval" } });
        }

        // CreateTopicByStaff_05 | B | AddAsync and SaveChangesAsync called once
        [Fact]
        public async Task Handle_ValidRequest_AddAndSaveCalledOnce()
        {
            var repo = GetRepoMock();
            await CreateHandler(repo: repo).Handle(MakeCmd(), CancellationToken.None);
            repo.Verify(x => x.AddAsync(It.IsAny<Topic>()), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Topic - Create By Staff", new TestCaseDetail { FunctionGroup = "CreateTopicByStaff", TestCaseID = "CreateTopicByStaff_05", Description = "AddAsync and SaveChangesAsync each called once", ExpectedResult = "Both Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Persist calls verified" } });
        }

        // CreateTopicByStaff_06 | N | CreateBy set to current userId from HttpContext
        [Fact]
        public async Task Handle_ValidRequest_CreateBySetToCurrentUserId()
        {
            Topic? captured = null;
            var repo = GetRepoMock();
            repo.Setup(x => x.AddAsync(It.IsAny<Topic>())).Callback<Topic>(t => captured = t).Returns(Task.CompletedTask);
            await CreateHandler(repo: repo, http: GetHttpContextMock("STAFF-XYZ")).Handle(MakeCmd(), CancellationToken.None);
            captured!.CreateBy.Should().Be("STAFF-XYZ");
            QACollector.LogTestCase("Topic - Create By Staff", new TestCaseDetail { FunctionGroup = "CreateTopicByStaff", TestCaseID = "CreateTopicByStaff_06", Description = "CreateBy='STAFF-XYZ' from HttpContext", ExpectedResult = "CreateBy='STAFF-XYZ'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "userId from claim mapped to CreateBy" } });
        }
    }
}