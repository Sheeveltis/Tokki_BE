using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailAutoTemplateById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates
{
    public class GetEmailAutoTemplateByIdQueryHandlerTests
    {
        private static GetEmailAutoTemplateByIdQueryHandler CreateHandler(
            Mock<IEmailTemplateRepository>? repo = null)
        {
            return new GetEmailAutoTemplateByIdQueryHandler(
                (repo ?? new Mock<IEmailTemplateRepository>()).Object);
        }

        private static EmailTemplate ActiveTemplate(string id = "TMPL-001") => new()
        {
            TemplateId   = id,
            TemplateName = "My Template",
            Status       = EmailTemplateStatus.Active,
            Type         = EmailTemplateType.OfflineReminder,
            Value        = 7
        };

        [Fact]
        public async Task Handle_EmptyTemplateId_ShouldReturnFailure()
        {
            var result = await CreateHandler().Handle(
                new GetEmailAutoTemplateByIdQuery { TemplateId = "" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Email Template - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailAutoTemplateById",
                TestCaseID        = "TC-EMAIL-GBI-01",
                Description       = "TemplateId is empty/whitespace → EmailTemplateNotFound",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "string.IsNullOrWhiteSpace(request.TemplateId)" }
            });
        }

        [Fact]
        public async Task Handle_TemplateNotFound_ShouldReturnFailure()
        {
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((EmailTemplate?)null);

            var result = await CreateHandler(mockRepo).Handle(
                new GetEmailAutoTemplateByIdQuery { TemplateId = "INVALID" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Email Template - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailAutoTemplateById",
                TestCaseID        = "TC-EMAIL-GBI-02",
                Description       = "TemplateId doesn't exist → EmailTemplateNotFound",
                ExpectedResult    = "Return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template == null" }
            });
        }

        [Fact]
        public async Task Handle_DeletedTemplate_ShouldReturnFailure()
        {
            var deleted = new EmailTemplate { TemplateId = "TMPL-001", Status = EmailTemplateStatus.Deleted };
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(deleted);

            var result = await CreateHandler(mockRepo).Handle(
                new GetEmailAutoTemplateByIdQuery { TemplateId = "TMPL-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Email Template - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailAutoTemplateById",
                TestCaseID        = "TC-EMAIL-GBI-03",
                Description       = "Template exists but Status=Deleted → treated as not found",
                ExpectedResult    = "Return Failure (soft-deleted considered missing)",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template.Status == Deleted => Failure" }
            });
        }

        [Fact]
        public async Task Handle_ActiveTemplate_ShouldReturnTemplate()
        {
            var template = ActiveTemplate();
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("TMPL-001")).ReturnsAsync(template);

            var result = await CreateHandler(mockRepo).Handle(
                new GetEmailAutoTemplateByIdQuery { TemplateId = "TMPL-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.TemplateId.Should().Be("TMPL-001");
            result.Data.Status.Should().Be(EmailTemplateStatus.Active);

            QACollector.LogTestCase("Email Template - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailAutoTemplateById",
                TestCaseID        = "TC-EMAIL-GBI-04",
                Description       = "Active template found → Return 200 with template entity",
                ExpectedResult    = "Return 200, Data = EmailTemplate",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template != null && Status != Deleted" }
            });
        }

        [Fact]
        public async Task Handle_DraftTemplate_ShouldReturnSuccessfully()
        {
            var draft = new EmailTemplate { TemplateId = "TMPL-002", TemplateName = "Draft", Status = EmailTemplateStatus.Draft };
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("TMPL-002")).ReturnsAsync(draft);

            var result = await CreateHandler(mockRepo).Handle(
                new GetEmailAutoTemplateByIdQuery { TemplateId = "TMPL-002" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Status.Should().Be(EmailTemplateStatus.Draft);

            QACollector.LogTestCase("Email Template - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailAutoTemplateById",
                TestCaseID        = "TC-EMAIL-GBI-05",
                Description       = "Draft template (not Deleted) → Return 200, Status=Draft",
                ExpectedResult    = "Return 200, Data.Status = Draft",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "template.Status == Draft => Success" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(
                    new GetEmailAutoTemplateByIdQuery { TemplateId = "TMPL-001" }, CancellationToken.None));

            QACollector.LogTestCase("Email Template - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailAutoTemplateById",
                TestCaseID        = "TC-EMAIL-GBI-06",
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
