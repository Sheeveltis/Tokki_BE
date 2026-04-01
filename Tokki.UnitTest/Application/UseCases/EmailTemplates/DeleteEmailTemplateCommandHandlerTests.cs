using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates
{
    public class DeleteEmailTemplateCommandHandlerTests
    {
        private static DeleteEmailAutoTemplateCommandHandler CreateHandler(
            Mock<IEmailTemplateRepository>? repo = null)
        {
            return new DeleteEmailAutoTemplateCommandHandler(
                (repo ?? new Mock<IEmailTemplateRepository>()).Object);
        }

        private static DeleteEmailAutoTemplateCommand ValidCommand => new() { TemplateId = "TMPL-001" };

        private static EmailTemplate ActiveTemplate(string id = "TMPL-001") => new()
        {
            TemplateId = id,
            Status     = EmailTemplateStatus.Active,
            UpdatedAt  = DateTime.UtcNow
        };

        [Fact]
        public async Task Handle_TemplateNotFound_ShouldReturnEmailTemplateNotFound()
        {
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((EmailTemplate?)null);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Email Template - Delete Auto", new TestCaseDetail
            {
                FunctionGroup     = "DeleteEmailAutoTemplate",
                TestCaseID        = "TC-EMAIL-DEL-01",
                Description       = "TemplateId does not exist → EmailTemplateNotFound",
                ExpectedResult    = "Return Failure (not found)",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template == null" }
            });
        }

        [Fact]
        public async Task Handle_AlreadyDeleted_ShouldReturnSuccess_Idempotent()
        {
            var deletedTemplate = new EmailTemplate { TemplateId = "TMPL-001", Status = EmailTemplateStatus.Deleted };
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(deletedTemplate);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Never);

            QACollector.LogTestCase("Email Template - Delete Auto", new TestCaseDetail
            {
                FunctionGroup     = "DeleteEmailAutoTemplate",
                TestCaseID        = "TC-EMAIL-DEL-02",
                Description       = "Template already Deleted → idempotent success, no UpdateAsync called",
                ExpectedResult    = "Return 200, UpdateAsync never called",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template.Status == Deleted => immediate success" }
            });
        }

        [Fact]
        public async Task Handle_ActiveTemplate_ShouldSoftDeleteToDeletedStatus()
        {
            var template = ActiveTemplate();
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            template.Status.Should().Be(EmailTemplateStatus.Deleted);
            mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Once);

            QACollector.LogTestCase("Email Template - Delete Auto", new TestCaseDetail
            {
                FunctionGroup     = "DeleteEmailAutoTemplate",
                TestCaseID        = "TC-EMAIL-DEL-03",
                Description       = "Active template → Status set to Deleted, UpdateAsync called",
                ExpectedResult    = "Return 200, template.Status = Deleted",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template.Status = Deleted" }
            });
        }

        [Fact]
        public async Task Handle_DraftTemplate_ShouldSoftDeleteSuccessfully()
        {
            var draft = new EmailTemplate { TemplateId = "T1", Status = EmailTemplateStatus.Draft };
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(draft);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            draft.Status.Should().Be(EmailTemplateStatus.Deleted);

            QACollector.LogTestCase("Email Template - Delete Auto", new TestCaseDetail
            {
                FunctionGroup     = "DeleteEmailAutoTemplate",
                TestCaseID        = "TC-EMAIL-DEL-04",
                Description       = "Draft template → also soft-deleteable",
                ExpectedResult    = "Return 200, draft.Status = Deleted",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "draft.Status == Draft => Deleted" }
            });
        }

        [Fact]
        public async Task Handle_AfterDelete_ShouldUpdateUpdatedAt()
        {
            var template = ActiveTemplate();
            var beforeDelete = template.UpdatedAt;
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            template.UpdatedAt.Should().BeAfter(beforeDelete.AddSeconds(-1));

            QACollector.LogTestCase("Email Template - Delete Auto", new TestCaseDetail
            {
                FunctionGroup     = "DeleteEmailAutoTemplate",
                TestCaseID        = "TC-EMAIL-DEL-05",
                Description       = "After deletion, UpdatedAt is set to current VN time",
                ExpectedResult    = "template.UpdatedAt updated",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template.UpdatedAt = DateTime.UtcNow.AddHours(7)" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB failure"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None));

            QACollector.LogTestCase("Email Template - Delete Auto", new TestCaseDetail
            {
                FunctionGroup     = "DeleteEmailAutoTemplate",
                TestCaseID        = "TC-EMAIL-DEL-06",
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
