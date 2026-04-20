using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using Tokki.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates
{
    public class CreateEmailAutoTemplateCommandHandlerTests
    {
        private static CreateEmailAutoTemplateCommandHandler CreateHandler(
            Mock<IEmailTemplateRepository>? repo = null)
        {
            return new CreateEmailAutoTemplateCommandHandler(
                (repo ?? new Mock<IEmailTemplateRepository>()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        private static CreateEmailAutoTemplateCommand ValidCommand => new()
        {
            TemplateName = "Welcome Template",
            Type         = EmailTemplateType.OfflineReminder,
            Value        = 7,
            TargetGroup  = UserTargetGroup.All,
            Subject      = "Welcome to Tokki!",
            Body         = "<p>Hello!</p>",
            Description  = "Auto welcome email"
        };

        [Fact]
        public async Task Handle_DuplicateTemplateName_ShouldReturnFailure()
        {
            var existing = new EmailTemplate { TemplateId = "T1", Status = EmailTemplateStatus.Active };
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByNameAsync(It.IsAny<string>())).ReturnsAsync(existing);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Email Template - Create Auto", new TestCaseDetail
            {
                FunctionGroup     = "CreateEmailAutoTemplate",
                TestCaseID        = "CreateEmailAutoTemplate_01",
                Description       = "TemplateName already exists (non-Deleted) → EmailTemplateKeyDuplicated",
                ExpectedResult    = "Return Failure (duplicate name)",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "existingByName != null && Status != Deleted" }
            });
        }

        [Fact]
        public async Task Handle_DuplicateLogicKey_ShouldReturnFailure()
        {
            var existing = new EmailTemplate { TemplateId = "T1", Status = EmailTemplateStatus.Draft };
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((EmailTemplate?)null);
            mockRepo.Setup(x => x.GetByTypeValueTargetAsync(It.IsAny<EmailTemplateType>(), It.IsAny<int>(), It.IsAny<UserTargetGroup>()))
                    .ReturnsAsync(existing);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Email Template - Create Auto", new TestCaseDetail
            {
                FunctionGroup     = "CreateEmailAutoTemplate",
                TestCaseID        = "CreateEmailAutoTemplate_02",
                Description       = "Type+Value+TargetGroup collision (non-Deleted) → EmailTemplateKeyDuplicated",
                ExpectedResult    = "Return Failure (logic key duplicate)",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "existingByLogic != null && Status != Deleted" }
            });
        }

        [Fact]
        public async Task Handle_DeletedNameConflict_ShouldAllowCreate()
        {
            var deletedTemplate = new EmailTemplate { TemplateId = "T-OLD", Status = EmailTemplateStatus.Deleted };
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByNameAsync(It.IsAny<string>())).ReturnsAsync(deletedTemplate);
            mockRepo.Setup(x => x.GetByTypeValueTargetAsync(It.IsAny<EmailTemplateType>(), It.IsAny<int>(), It.IsAny<UserTargetGroup>()))
                    .ReturnsAsync((EmailTemplate?)null);
            mockRepo.Setup(x => x.AddAsync(It.IsAny<EmailTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);

            QACollector.LogTestCase("Email Template - Create Auto", new TestCaseDetail
            {
                FunctionGroup     = "CreateEmailAutoTemplate",
                TestCaseID        = "CreateEmailAutoTemplate_03",
                Description       = "Existing template with same name but Status=Deleted → allowed to create",
                ExpectedResult    = "Return 201 Success",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "existingByName.Status == Deleted => skip check" }
            });
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateDraftTemplate()
        {
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((EmailTemplate?)null);
            mockRepo.Setup(x => x.GetByTypeValueTargetAsync(It.IsAny<EmailTemplateType>(), It.IsAny<int>(), It.IsAny<UserTargetGroup>()))
                    .ReturnsAsync((EmailTemplate?)null);
            mockRepo.Setup(x => x.AddAsync(It.IsAny<EmailTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            mockRepo.Verify(x => x.AddAsync(It.Is<EmailTemplate>(t => t.Status == EmailTemplateStatus.Draft)), Times.Once);

            QACollector.LogTestCase("Email Template - Create Auto", new TestCaseDetail
            {
                FunctionGroup     = "CreateEmailAutoTemplate",
                TestCaseID        = "CreateEmailAutoTemplate_04",
                Description       = "Happy path: template created successfully with Status=Draft",
                ExpectedResult    = "Return 201, Status=Draft",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No conflicts → template.Status = Draft" }
            });
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldGenerateTemplateId()
        {
            string? capturedId = null;
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((EmailTemplate?)null);
            mockRepo.Setup(x => x.GetByTypeValueTargetAsync(It.IsAny<EmailTemplateType>(), It.IsAny<int>(), It.IsAny<UserTargetGroup>()))
                    .ReturnsAsync((EmailTemplate?)null);
            mockRepo.Setup(x => x.AddAsync(It.IsAny<EmailTemplate>()))
                    .Callback<EmailTemplate>(t => capturedId = t.TemplateId)
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            capturedId.Should().NotBeNullOrEmpty();
            result.Data.Should().Be(capturedId);

            QACollector.LogTestCase("Email Template - Create Auto", new TestCaseDetail
            {
                FunctionGroup     = "CreateEmailAutoTemplate",
                TestCaseID        = "CreateEmailAutoTemplate_05",
                Description       = "TemplateId generated by IdGeneratorService and returned as Data",
                ExpectedResult    = "Result.Data = generated TemplateId",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template.TemplateId = _idGenerator.Generate(15)" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByNameAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB timeout"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None));

            QACollector.LogTestCase("Email Template - Create Auto", new TestCaseDetail
            {
                FunctionGroup     = "CreateEmailAutoTemplate",
                TestCaseID        = "CreateEmailAutoTemplate_06",
                Description       = "Repository throws → exception propagates",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByNameAsync throws" }
            });
        }
    }
}