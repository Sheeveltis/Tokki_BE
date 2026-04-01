using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetAllEmailTemplates;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates
{
    public class GetAllEmailAutoTemplatesQueryHandlerTests
    {
        private static GetAllEmailAutoTemplatesQueryHandler CreateHandler(
            Mock<IEmailTemplateRepository>? repo = null)
        {
            return new GetAllEmailAutoTemplatesQueryHandler(
                (repo ?? new Mock<IEmailTemplateRepository>()).Object);
        }

        private static GetAllEmailAutoTemplatesQuery DefaultQuery => new()
        {
            PageNumber = 1,
            PageSize   = 10
        };

        private static EmailTemplate BuildTemplate(string id, EmailTemplateStatus status = EmailTemplateStatus.Active) => new()
        {
            TemplateId   = id,
            TemplateName = $"Template-{id}",
            Status       = status,
            Type         = EmailTemplateType.OfflineReminder,
            Value        = 7,
            CreateAt     = DateTime.UtcNow
        };

        [Fact]
        public async Task Handle_EmptyRepository_ShouldReturnEmptyPagedResult()
        {
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                    .ReturnsAsync((new List<EmailTemplate>(), 0));

            var result = await CreateHandler(mockRepo).Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Email Template - Get All", new TestCaseDetail
            {
                FunctionGroup     = "GetAllEmailAutoTemplates",
                TestCaseID        = "TC-EMAIL-GALL-01",
                Description       = "Repository empty → Return 200, Items empty",
                ExpectedResult    = "Return 200, Items = []",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "allTemplates.Count == 0" }
            });
        }

        [Fact]
        public async Task Handle_NoStatusFilter_ShouldExcludeDeletedTemplates()
        {
            var templates = new List<EmailTemplate>
            {
                BuildTemplate("T1", EmailTemplateStatus.Active),
                BuildTemplate("T2", EmailTemplateStatus.Draft),
                BuildTemplate("T3", EmailTemplateStatus.Deleted)
            };

            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                    .ReturnsAsync((templates, 3));

            var result = await CreateHandler(mockRepo).Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items.Should().NotContain(t => t.Status == EmailTemplateStatus.Deleted);

            QACollector.LogTestCase("Email Template - Get All", new TestCaseDetail
            {
                FunctionGroup     = "GetAllEmailAutoTemplates",
                TestCaseID        = "TC-EMAIL-GALL-02",
                Description       = "No Status filter → Deleted templates excluded by default",
                ExpectedResult    = "Return 200, Items exclude Deleted (2 out of 3)",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!request.Status.HasValue => exclude Deleted" }
            });
        }

        [Fact]
        public async Task Handle_StatusFilter_ShouldReturnOnlyMatchingStatus()
        {
            var templates = new List<EmailTemplate>
            {
                BuildTemplate("T1", EmailTemplateStatus.Active),
                BuildTemplate("T2", EmailTemplateStatus.Draft),
                BuildTemplate("T3", EmailTemplateStatus.Deleted)
            };

            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                    .ReturnsAsync((templates, 3));

            var query = new GetAllEmailAutoTemplatesQuery { PageNumber = 1, PageSize = 10, Status = EmailTemplateStatus.Draft };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items[0].Status.Should().Be(EmailTemplateStatus.Draft);

            QACollector.LogTestCase("Email Template - Get All", new TestCaseDetail
            {
                FunctionGroup     = "GetAllEmailAutoTemplates",
                TestCaseID        = "TC-EMAIL-GALL-03",
                Description       = "Status=Draft filter → only Draft templates returned",
                ExpectedResult    = "Return 200, Items.Count=1, Status=Draft",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Status.HasValue => filter by Status" }
            });
        }

        [Fact]
        public async Task Handle_SearchName_ShouldReturnMatchingTemplates()
        {
            var templates = new List<EmailTemplate>
            {
                new() { TemplateId = "T1", TemplateName = "Welcome Email", Status = EmailTemplateStatus.Active, CreateAt = DateTime.UtcNow, Type = EmailTemplateType.OfflineReminder, Value = 1 },
                new() { TemplateId = "T2", TemplateName = "Reminder Notice", Status = EmailTemplateStatus.Active, CreateAt = DateTime.UtcNow, Type = EmailTemplateType.VipExpiringReminder, Value = 2 }
            };

            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue)).ReturnsAsync((templates, 2));

            var query = new GetAllEmailAutoTemplatesQuery { PageNumber = 1, PageSize = 10, SearchName = "welcome" };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items[0].TemplateName.Should().Contain("Welcome");

            QACollector.LogTestCase("Email Template - Get All", new TestCaseDetail
            {
                FunctionGroup     = "GetAllEmailAutoTemplates",
                TestCaseID        = "TC-EMAIL-GALL-04",
                Description       = "SearchName='welcome' (case-insensitive) → only matching template",
                ExpectedResult    = "Return 200, Items.Count=1, TemplateName contains 'welcome'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TemplateName.ToLower().Contains('welcome')" }
            });
        }

        [Fact]
        public async Task Handle_PaginationApplied_ShouldReturnCorrectPage()
        {
            var templates = Enumerable.Range(1, 15)
                .Select(i => BuildTemplate($"T{i}"))
                .ToList();

            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(1, int.MaxValue)).ReturnsAsync((templates, 15));

            var query = new GetAllEmailAutoTemplatesQuery { PageNumber = 2, PageSize = 5 };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(5);
            result.Data.TotalCount.Should().Be(15);
            result.Data.TotalPages.Should().Be(3);

            QACollector.LogTestCase("Email Template - Get All", new TestCaseDetail
            {
                FunctionGroup     = "GetAllEmailAutoTemplates",
                TestCaseID        = "TC-EMAIL-GALL-05",
                Description       = "Page 2 of 15 items with PageSize=5 → Items=5, TotalPages=3",
                ExpectedResult    = "Return 200, Items=5, TotalPages=3",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skip((2-1)*5).Take(5)" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IEmailTemplateRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>()))
                    .ThrowsAsync(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(DefaultQuery, CancellationToken.None));

            QACollector.LogTestCase("Email Template - Get All", new TestCaseDetail
            {
                FunctionGroup     = "GetAllEmailAutoTemplates",
                TestCaseID        = "TC-EMAIL-GALL-06",
                Description       = "Repository throws → exception propagates",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedAsync throws" }
            });
        }
    }
}
