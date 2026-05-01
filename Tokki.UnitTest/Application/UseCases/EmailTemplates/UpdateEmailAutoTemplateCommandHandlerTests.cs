using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates
{
    public class UpdateEmailAutoTemplateCommandHandlerTests
    {
        private static UpdateEmailAutoTemplateCommandHandler CreateHandler(
            Mock<IEmailTemplateRepository>? repo = null)
        {
            return new UpdateEmailAutoTemplateCommandHandler(
                (repo ?? new Mock<IEmailTemplateRepository>()).Object);
        }

        private static EmailTemplate ExistingTemplate(string id = "TMPL-001") => new()
        {
            TemplateId   = id,
            TemplateName = "Old Name",
            Type         = EmailTemplateType.OfflineReminder,
            Value        = 7,
            TargetGroup  = UserTargetGroup.All,
            Subject      = "Old Subject",
            Body         = "<p>Old</p>",
            Status       = EmailTemplateStatus.Active,
            UpdatedAt    = DateTime.UtcNow
        };

        [Fact]
        public async Task Handle_TemplateNotFound_ShouldReturnFailure()
        {
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((EmailTemplate?)null);

            var result = await CreateHandler(mockRepo).Handle(
                new UpdateEmailAutoTemplateCommand { TemplateId = "INVALID" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup     = "UpdateEmailAutoTemplate",
                TestCaseID        = "UpdateEmailAutoTemplate_01",
                Description       = "TemplateId does not exist → EmailTemplateNotFound",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template == null" }
            });
        }

        [Fact]
        public async Task Handle_DuplicateTemplateName_ShouldReturnFailure()
        {
            var template = ExistingTemplate();
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("TMPL-001")).ReturnsAsync(template);
            mockRepo.Setup(x => x.GetByNameAsync("New Name"))
                    .ReturnsAsync(new EmailTemplate { TemplateId = "OTHER-TMPL", Status = EmailTemplateStatus.Active });

            var cmd = new UpdateEmailAutoTemplateCommand { TemplateId = "TMPL-001", TemplateName = "New Name" };
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup     = "UpdateEmailAutoTemplate",
                TestCaseID        = "UpdateEmailAutoTemplate_02",
                Description       = "New TemplateName already used by another template → Duplicate",
                ExpectedResult    = "Return Failure (duplicate name)",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "existingByName.TemplateId != current.TemplateId" }
            });
        }

        [Fact]
        public async Task Handle_NothingChanged_ShouldReturnSuccessWithoutSaving()
        {
            var template = ExistingTemplate();
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("TMPL-001")).ReturnsAsync(template);

            var cmd = new UpdateEmailAutoTemplateCommand { TemplateId = "TMPL-001" };
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Never);

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup     = "UpdateEmailAutoTemplate",
                TestCaseID        = "UpdateEmailAutoTemplate_03",
                Description       = "No fields changed → Success without UpdateAsync called",
                ExpectedResult    = "Return 200, UpdateAsync never called",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!anyChanged => early return Success" }
            });
        }

        [Fact]
        public async Task Handle_ValidUpdateSubject_ShouldPersistChange()
        {
            var template = ExistingTemplate();
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("TMPL-001")).ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var cmd = new UpdateEmailAutoTemplateCommand { TemplateId = "TMPL-001", Subject = "New Subject" };
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.Subject.Should().Be("New Subject");
            mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Once);

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup     = "UpdateEmailAutoTemplate",
                TestCaseID        = "UpdateEmailAutoTemplate_04",
                Description       = "Subject changed → template.Subject updated, UpdateAsync called",
                ExpectedResult    = "Return 200, template.Subject = 'New Subject'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Subject != template.Subject => template.Subject = request.Subject" }
            });
        }

        [Fact]
        public async Task Handle_ValidUpdateStatus_ShouldChangeStatus()
        {
            var template = ExistingTemplate();
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("TMPL-001")).ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var cmd = new UpdateEmailAutoTemplateCommand { TemplateId = "TMPL-001", Status = EmailTemplateStatus.Draft };
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.Status.Should().Be(EmailTemplateStatus.Draft);

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup     = "UpdateEmailAutoTemplate",
                TestCaseID        = "UpdateEmailAutoTemplate_05",
                Description       = "Status changed from Active → Draft",
                ExpectedResult    = "Return 200, template.Status = Draft",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Status != template.Status => template.Status = Draft" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB failure"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(new UpdateEmailAutoTemplateCommand { TemplateId = "T1" }, CancellationToken.None));

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup     = "UpdateEmailAutoTemplate",
                TestCaseID        = "UpdateEmailAutoTemplate_06",
                Description       = "Repository throws → exception propagates",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync throws" }
            });
        }
    }
}
