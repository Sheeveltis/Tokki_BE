using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates
{
    public class DeleteEmailAutoTemplateCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static DeleteEmailAutoTemplateCommandHandler CreateHandler(Mock<IEmailTemplateRepository>? repo = null)
        {
            var mockRepo = repo ?? new Mock<IEmailTemplateRepository>();
            return new DeleteEmailAutoTemplateCommandHandler(mockRepo.Object);
        }

        private static EmailTemplate GetSampleTemplate(
            string id     = "TMPL-001",
            EmailTemplateStatus status = EmailTemplateStatus.Active) => new()
        {
            TemplateId   = id,
            TemplateName = "Sample Template",
            Status       = status
        };

        // ═══════════════════════════════════════════════════════════
        // TC-ETD-01 | A | Template not found → Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TemplateNotFound_ShouldReturnFailure()
        {
            // Arrange
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((EmailTemplate?)null);

            var command = new DeleteEmailAutoTemplateCommand { TemplateId = "TMPL-GHOST" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(AppErrors.EmailTemplateNotFound);

            QACollector.LogTestCase("Email Template - Delete Auto", new TestCaseDetail
            {
                FunctionGroup     = "Delete Email Auto Template",
                TestCaseID        = "TC-ETD-01",
                Description       = "Attempt to delete a template that does not exist",
                ExpectedResult    = "Return Failure EmailTemplateNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return Failure" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ETD-02 | N | Already deleted → idempotent 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AlreadyDeleted_ShouldReturn200Idempotent()
        {
            // Arrange
            var template = GetSampleTemplate(status: EmailTemplateStatus.Deleted);
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("TMPL-001")).ReturnsAsync(template);

            var command = new DeleteEmailAutoTemplateCommand { TemplateId = "TMPL-001" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            QACollector.LogTestCase("Email Template - Delete Auto", new TestCaseDetail
            {
                FunctionGroup     = "Delete Email Auto Template",
                TestCaseID        = "TC-ETD-02",
                Description       = "Delete a template that is already in Deleted status (idempotent)",
                ExpectedResult    = "Return 200 Success (already deleted message)",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status == Deleted", "Idempotent path returns Success" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ETD-03 | N | Active template → soft-deleted 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ActiveTemplate_ShouldSoftDeleteAndReturn200()
        {
            // Arrange
            var template = GetSampleTemplate(status: EmailTemplateStatus.Active);
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("TMPL-001")).ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var command = new DeleteEmailAutoTemplateCommand { TemplateId = "TMPL-001" };

            // Act
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            template.Status.Should().Be(EmailTemplateStatus.Deleted);

            QACollector.LogTestCase("Email Template - Delete Auto", new TestCaseDetail
            {
                FunctionGroup     = "Delete Email Auto Template",
                TestCaseID        = "TC-ETD-03",
                Description       = "Delete an active template — status should be set to Deleted",
                ExpectedResult    = "Return 200, template.Status = Deleted",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status was Active", "Status mutated to Deleted", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ETD-04 | N | UpdateAsync is called exactly once on valid delete
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ActiveTemplate_UpdateAsyncCalledOnce()
        {
            // Arrange
            var template = GetSampleTemplate(status: EmailTemplateStatus.Active);
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("TMPL-001")).ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var command = new DeleteEmailAutoTemplateCommand { TemplateId = "TMPL-001" };

            // Act
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Once);

            QACollector.LogTestCase("Email Template - Delete Auto", new TestCaseDetail
            {
                FunctionGroup     = "Delete Email Auto Template",
                TestCaseID        = "TC-ETD-04",
                Description       = "Verify UpdateAsync is invoked exactly once when performing soft delete",
                ExpectedResult    = "UpdateAsync called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Moq.Verify confirms single UpdateAsync call" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ETD-05 | N | Already deleted → UpdateAsync is NOT called
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AlreadyDeleted_UpdateAsyncNotCalled()
        {
            // Arrange
            var template = GetSampleTemplate(status: EmailTemplateStatus.Deleted);
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("TMPL-001")).ReturnsAsync(template);

            var command = new DeleteEmailAutoTemplateCommand { TemplateId = "TMPL-001" };

            // Act
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Never);

            QACollector.LogTestCase("Email Template - Delete Auto", new TestCaseDetail
            {
                FunctionGroup     = "Delete Email Auto Template",
                TestCaseID        = "TC-ETD-05",
                Description       = "When template is already Deleted, UpdateAsync should never be called",
                ExpectedResult    = "UpdateAsync not called (idempotent short circuit)",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status == Deleted", "Idempotent early return", "No UpdateAsync call" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ETD-06 | A | SaveChanges throws → exception propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SaveChangesThrows_ShouldPropagateException()
        {
            // Arrange
            var template = GetSampleTemplate(status: EmailTemplateStatus.Active);
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("TMPL-001")).ReturnsAsync(template);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailTemplate>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB Failure"));

            var command = new DeleteEmailAutoTemplateCommand { TemplateId = "TMPL-001" };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo).Handle(command, CancellationToken.None));

            QACollector.LogTestCase("Email Template - Delete Auto", new TestCaseDetail
            {
                FunctionGroup     = "Delete Email Auto Template",
                TestCaseID        = "TC-ETD-06",
                Description       = "SaveChangesAsync throws during soft delete persistence",
                ExpectedResult    = "Exception propagates to global middleware",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SaveChangesAsync throws Exception", "No try/catch in handler" }
            });
        }
    }
}
