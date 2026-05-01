using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.ExamTemplates
{
    public class CreateExamTemplateCommandHandlerTests
    {
        private static CreateExamTemplateCommandHandler CreateHandler(
            Mock<IExamTemplateRepository>? repo = null,
            Mock<IHttpContextAccessor>? httpCtx = null)
        {
            return new CreateExamTemplateCommandHandler(
                (repo ?? new Mock<IExamTemplateRepository>()).Object,
                MockIdGeneratorService.GetMock().Object,
                (httpCtx ?? new Mock<IHttpContextAccessor>()).Object);
        }

        private static Mock<IHttpContextAccessor> BuildHttpCtx(string? userId)
        {
            var mock = new Mock<IHttpContextAccessor>();
            if (userId == null)
            {
                mock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            }
            else
            {
                var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
                var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
                mock.Setup(x => x.HttpContext).Returns(ctx);
            }
            return mock;
        }

        private static CreateExamTemplateCommand ValidCommand => new()
        {
            Name        = "TOEIC Template",
            Description = "Standard TOEIC test",
            Type        = ExamType.TopikI,
            CreatedBy   = "ADMIN-001"
        };

        [Fact]
        public async Task Handle_DuplicateName_ShouldReturnFailure()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(true);

            var result = await CreateHandler(repo: mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("tồn tại");

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateExamTemplate",
                TestCaseID        = "CreateExamTemplate_01",
                Description       = "Template name already exists → Failure 'Tên đề thi mẫu đã tồn tại'",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsNameExistsAsync = true" }
            });
        }

        [Fact]
        public async Task Handle_ValidCommandWithCreatedBy_ShouldCreateDraftTemplate()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
            mockRepo.Setup(x => x.AddAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            ExamTemplate? capturedTemplate = null;
            mockRepo.Setup(x => x.AddAsync(It.IsAny<ExamTemplate>()))
                    .Callback<ExamTemplate>(t => capturedTemplate = t)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler(repo: mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            capturedTemplate!.Status.Should().Be(ExamTemplateStatus.Draft);
            capturedTemplate.CreatedBy.Should().Be("ADMIN-001");

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateExamTemplate",
                TestCaseID        = "CreateExamTemplate_02",
                Description       = "Valid command with CreatedBy → template created with Status=Draft",
                ExpectedResult    = "Return Success, Status=Draft, CreatedBy='ADMIN-001'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Name unique, CreatedBy provided" }
            });
        }

        [Fact]
        public async Task Handle_NoCreatedBy_ShouldUseHttpContextUserId()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
            mockRepo.Setup(x => x.AddAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            ExamTemplate? capturedTemplate = null;
            mockRepo.Setup(x => x.AddAsync(It.IsAny<ExamTemplate>()))
                    .Callback<ExamTemplate>(t => capturedTemplate = t)
                    .Returns(Task.CompletedTask);

            var cmd = new CreateExamTemplateCommand
            {
                Name      = "TOEIC Template",
                Type      = ExamType.TopikI,
                CreatedBy = "" // vide → fallback HttpContext
            };

            var result = await CreateHandler(repo: mockRepo, httpCtx: BuildHttpCtx("CTX-USER-001"))
                             .Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            capturedTemplate!.CreatedBy.Should().Be("CTX-USER-001");

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateExamTemplate",
                TestCaseID        = "CreateExamTemplate_03",
                Description       = "CreatedBy empty → fallback to HttpContext NameIdentifier claim",
                ExpectedResult    = "CreatedBy = 'CTX-USER-001' from token",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CreatedBy == '' => HttpContext.User.NameIdentifier" }
            });
        }

        [Fact]
        public async Task Handle_NoCreatedBy_NoHttpContext_ShouldFallbackToSystem()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
            mockRepo.Setup(x => x.AddAsync(It.IsAny<ExamTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            ExamTemplate? capturedTemplate = null;
            mockRepo.Setup(x => x.AddAsync(It.IsAny<ExamTemplate>()))
                    .Callback<ExamTemplate>(t => capturedTemplate = t)
                    .Returns(Task.CompletedTask);

            var cmd = new CreateExamTemplateCommand
            {
                Name      = "TOEIC Template",
                Type      = ExamType.TopikI,
                CreatedBy = ""
            };

            var result = await CreateHandler(repo: mockRepo, httpCtx: BuildHttpCtx(null))
                             .Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            capturedTemplate!.CreatedBy.Should().Be("SYSTEM");

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateExamTemplate",
                TestCaseID        = "CreateExamTemplate_04",
                Description       = "No CreatedBy, no HttpContext → fallback to 'SYSTEM'",
                ExpectedResult    = "CreatedBy = 'SYSTEM'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "userId == null => 'SYSTEM'" }
            });
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldReturnGeneratedId()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);

            string? capturedId = null;
            mockRepo.Setup(x => x.AddAsync(It.IsAny<ExamTemplate>()))
                    .Callback<ExamTemplate>(t => capturedId = t.ExamTemplateId)
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateHandler(repo: mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
            result.Data.Should().Be(capturedId);

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateExamTemplate",
                TestCaseID        = "CreateExamTemplate_05",
                Description       = "Valid command → ExamTemplateId generated and returned as Data",
                ExpectedResult    = "Result.Data = generated ExamTemplateId",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "newId = _idGeneratorService.GenerateCustom(10)" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IExamTemplateRepository>();
            mockRepo.Setup(x => x.IsNameExistsAsync(It.IsAny<string>(), null)).ThrowsAsync(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(repo: mockRepo).Handle(ValidCommand, CancellationToken.None));

            QACollector.LogTestCase("Exam Template - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateExamTemplate",
                TestCaseID        = "CreateExamTemplate_06",
                Description       = "Repository throws → exception propagates",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsNameExistsAsync throws" }
            });
        }
    }
}
