using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetAllEmailTemplates;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates.Queries
{
    public class GetAllEmailAutoTemplatesQueryHandlerTests
    {
        private readonly Mock<IEmailTemplateRepository> _repoMock;

        public GetAllEmailAutoTemplatesQueryHandlerTests()
        {
            _repoMock = new Mock<IEmailTemplateRepository>();
        }

        private GetAllEmailAutoTemplatesQueryHandler CreateHandler()
        {
            return new GetAllEmailAutoTemplatesQueryHandler(_repoMock.Object);
        }

        private void SetupRepository(List<EmailTemplate> data)
        {
            _repoMock.Setup(x => x.GetPagedAsync(1, int.MaxValue))
                     .ReturnsAsync((data, data.Count));
        }

        // ═══════════════════════════════════════════════════════════
        // GetAllEmailAutoTemplatesQueryHandler_01 | N | Filter Default Excludes Deleted
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoStatus_ShouldExcludeDeleted()
        {
            var data = new List<EmailTemplate>
            {
                new EmailTemplate { TemplateId = "1", Status = EmailTemplateStatus.Active },
                new EmailTemplate { TemplateId = "2", Status = EmailTemplateStatus.Deleted }
            };
            SetupRepository(data);
            var handler = CreateHandler();
            
            var query = new GetAllEmailAutoTemplatesQuery { PageNumber = 1, PageSize = 10, Status = null };
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items.First().TemplateId.Should().Be("1");

            QACollector.LogTestCase("Email Template - Get All", new TestCaseDetail
            {
                FunctionGroup = "GetAllEmailAutoTemplatesQueryHandler",
                TestCaseID = "GetAllEmailAutoTemplatesQueryHandler_01",
                Description = "Default behavior excludes items with Deleted status automatically",
                ExpectedResult = "Return items without Deleted",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status param is null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetAllEmailAutoTemplatesQueryHandler_02 | N | Filter by Exact Status
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ByStatus_ShouldReturnMatching()
        {
            var data = new List<EmailTemplate>
            {
                new EmailTemplate { TemplateId = "1", Status = EmailTemplateStatus.Draft },
                new EmailTemplate { TemplateId = "2", Status = EmailTemplateStatus.Deleted }
            };
            SetupRepository(data);
            var handler = CreateHandler();
            
            var query = new GetAllEmailAutoTemplatesQuery { PageNumber = 1, PageSize = 10, Status = EmailTemplateStatus.Deleted };
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items.First().TemplateId.Should().Be("2");

            QACollector.LogTestCase("Email Template - Get All", new TestCaseDetail
            {
                FunctionGroup = "GetAllEmailAutoTemplatesQueryHandler",
                TestCaseID = "GetAllEmailAutoTemplatesQueryHandler_02",
                Description = "Specific Status query retrieves specifically that state, even if deleted",
                ExpectedResult = "Return explicitly matched statuses",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Deleted param" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetAllEmailAutoTemplatesQueryHandler_03 | N | Filter Type and SearchName
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TypeAndSearchName_ShouldFilterCorrectly()
        {
            var data = new List<EmailTemplate>
            {
                new EmailTemplate { TemplateId = "1", Type = EmailTemplateType.OfflineReminder, TemplateName = "Abc Sales" },
                new EmailTemplate { TemplateId = "2", Type = EmailTemplateType.OfflineReminder, TemplateName = "xyz" },
                new EmailTemplate { TemplateId = "3", Type = EmailTemplateType.VipExpiringReminder, TemplateName = "Abc System" }
            };
            SetupRepository(data);
            var handler = CreateHandler();
            
            var query = new GetAllEmailAutoTemplatesQuery 
            { 
                PageNumber = 1, PageSize = 10, Type = EmailTemplateType.OfflineReminder, SearchName = "abc" 
            };
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items.First().TemplateId.Should().Be("1");

            QACollector.LogTestCase("Email Template - Get All", new TestCaseDetail
            {
                FunctionGroup = "GetAllEmailAutoTemplatesQueryHandler",
                TestCaseID = "GetAllEmailAutoTemplatesQueryHandler_03",
                Description = "Case insensitive searchName coupled with Type param filters",
                ExpectedResult = "Filtered List properly",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Type matched and Name Contains 'abc'" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetAllEmailAutoTemplatesQueryHandler_04 | N | Filter TargetGroup and Value
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TargetGroupAndValue_ShouldFilterCorrectly()
        {
            var data = new List<EmailTemplate>
            {
                new EmailTemplate { TemplateId = "1", TargetGroup = UserTargetGroup.All, Value = 10 },
                new EmailTemplate { TemplateId = "2", TargetGroup = UserTargetGroup.FreeUsers, Value = 10 },
                new EmailTemplate { TemplateId = "3", TargetGroup = UserTargetGroup.All, Value = 5 }
            };
            SetupRepository(data);
            var handler = CreateHandler();
            
            var query = new GetAllEmailAutoTemplatesQuery 
            { 
                PageNumber = 1, PageSize = 10, TargetGroup = UserTargetGroup.All, Value = 10 
            };
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items.First().TemplateId.Should().Be("1");

            QACollector.LogTestCase("Email Template - Get All", new TestCaseDetail
            {
                FunctionGroup = "GetAllEmailAutoTemplatesQueryHandler",
                TestCaseID = "GetAllEmailAutoTemplatesQueryHandler_04",
                Description = "Filters properly matching TargetGroup Enums and integer Values",
                ExpectedResult = "Filtered list properly",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TargetGroup and Value combined" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetAllEmailAutoTemplatesQueryHandler_05 | N | SearchSubject and Sort Order
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SearchSubject_RetrievesDescendingOrder()
        {
            var data = new List<EmailTemplate>
            {
                new EmailTemplate { TemplateId = "1", Subject = "Sale", CreateAt = DateTime.UtcNow },
                new EmailTemplate { TemplateId = "2", Subject = "Top Sale", CreateAt = DateTime.UtcNow.AddDays(1) },
                new EmailTemplate { TemplateId = "3", Subject = "Hello", CreateAt = DateTime.UtcNow.AddDays(2) }
            };
            SetupRepository(data);
            var handler = CreateHandler();
            
            var query = new GetAllEmailAutoTemplatesQuery { PageNumber = 1, PageSize = 10, SearchSubject = "sale" };
            var result = await handler.Handle(query, CancellationToken.None);

            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items.First().TemplateId.Should().Be("2"); // because CreateAt is bigger

            QACollector.LogTestCase("Email Template - Get All", new TestCaseDetail
            {
                FunctionGroup = "GetAllEmailAutoTemplatesQueryHandler",
                TestCaseID = "GetAllEmailAutoTemplatesQueryHandler_05",
                Description = "Subject string search works and default descending order is maintained",
                ExpectedResult = "List returned newest first",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Search by subject substring case insenstive" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetAllEmailAutoTemplatesQueryHandler_06 | N | Pagination Skip and Take
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Pagination_AppliesSkipTake()
        {
            var data = new List<EmailTemplate>
            {
                new EmailTemplate { TemplateId = "1", CreateAt = DateTime.UtcNow.AddDays(3) },
                new EmailTemplate { TemplateId = "2", CreateAt = DateTime.UtcNow.AddDays(2) },
                new EmailTemplate { TemplateId = "3", CreateAt = DateTime.UtcNow.AddDays(1) }
            };
            SetupRepository(data);
            var handler = CreateHandler();
            
            var query = new GetAllEmailAutoTemplatesQuery { PageNumber = 2, PageSize = 1 };
            var result = await handler.Handle(query, CancellationToken.None);

            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items.First().TemplateId.Should().Be("2"); // skips first largest element, takes 2nd

            QACollector.LogTestCase("Email Template - Get All", new TestCaseDetail
            {
                FunctionGroup = "GetAllEmailAutoTemplatesQueryHandler",
                TestCaseID = "GetAllEmailAutoTemplatesQueryHandler_06",
                Description = "PageNumber > 1 accurately offsets data results array",
                ExpectedResult = "Data is paginated properly",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PageNumber = 2, Size = 1" }
            });
        }
    }
}
