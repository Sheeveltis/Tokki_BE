using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailCampaigns;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates
{
    public class GetEmailCampaignsQueryHandlerTests
    {
        private static GetEmailCampaignsQueryHandler CreateHandler(
            Mock<IEmailJobRepository>? jobRepo = null)
        {
            return new GetEmailCampaignsQueryHandler(
                (jobRepo ?? new Mock<IEmailJobRepository>()).Object);
        }

        private static GetEmailCampaignsQuery DefaultQuery => new()
        {
            PageNumber = 1,
            PageSize   = 10
        };

        private static EmailJob BuildJob(string id, EmailJobStatus status = EmailJobStatus.Pending) => new()
        {
            JobId         = id,
            Status        = status,
            Subject       = $"Email Subject {id}",
            TargetGroup   = UserTargetGroup.All,
            ScheduledTime = DateTime.UtcNow,
            CreatedAt     = DateTime.UtcNow
        };

        [Fact]
        public async Task Handle_EmptyRepository_ShouldReturnEmptyPagedResult()
        {
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<EmailJobStatus?>(), It.IsAny<UserTargetGroup?>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                    It.IsAny<string?>(), It.IsAny<bool>()))
                    .ReturnsAsync((new List<EmailJob>(), 0));

            var result = await CreateHandler(mockRepo).Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Email - Get Campaigns List", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailCampaigns",
                TestCaseID        = "GetEmailCampaigns_01",
                Description       = "No jobs in DB → Return 200, Items empty",
                ExpectedResult    = "Return 200, Items = []",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "total == 0" }
            });
        }

        [Fact]
        public async Task Handle_MultipleJobs_ShouldReturnPagedResult()
        {
            var jobs = new List<EmailJob> { BuildJob("J1"), BuildJob("J2", EmailJobStatus.Sent) };
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<EmailJobStatus?>(), It.IsAny<UserTargetGroup?>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                    It.IsAny<string?>(), It.IsAny<bool>()))
                    .ReturnsAsync((jobs, 2));

            var result = await CreateHandler(mockRepo).Handle(DefaultQuery, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);

            QACollector.LogTestCase("Email - Get Campaigns List", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailCampaigns",
                TestCaseID        = "GetEmailCampaigns_02",
                Description       = "2 jobs found → Return 200, Items=2",
                ExpectedResult    = "Return 200, Items.Count=2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "items.Count == 2" }
            });
        }

        [Fact]
        public async Task Handle_PaginationQuery_ShouldReturnCorrectMetadata()
        {
            var fiveJobs = new List<EmailJob> { BuildJob("J1"), BuildJob("J2"), BuildJob("J3"), BuildJob("J4"), BuildJob("J5") };
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(
                    2, 5, It.IsAny<EmailJobStatus?>(), It.IsAny<UserTargetGroup?>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                    It.IsAny<string?>(), It.IsAny<bool>()))
                    .ReturnsAsync((fiveJobs, 20));

            var query = new GetEmailCampaignsQuery { PageNumber = 2, PageSize = 5 };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.Data!.TotalCount.Should().Be(20);
            result.Data.TotalPages.Should().Be(4);
            result.Data.PageNumber.Should().Be(2);

            QACollector.LogTestCase("Email - Get Campaigns List", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailCampaigns",
                TestCaseID        = "GetEmailCampaigns_03",
                Description       = "Page 2/5, 20 total → TotalPages=4",
                ExpectedResult    = "Return 200, TotalPages=4, PageNumber=2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PagedResult.Create(items, 20, 2, 5)" }
            });
        }

        [Fact]
        public async Task Handle_StatusFilter_ShouldPassToRepository()
        {
            var pendingJobs = new List<EmailJob> { BuildJob("J1") };
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(
                    1, 10, EmailJobStatus.Pending, null, null, null, null, null, null, false))
                    .ReturnsAsync((pendingJobs, 1));

            var query = new GetEmailCampaignsQuery { PageNumber = 1, PageSize = 10, Status = EmailJobStatus.Pending };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            mockRepo.Verify(x => x.GetPagedAsync(1, 10, EmailJobStatus.Pending, null, null, null, null, null, null, false), Times.Once);

            QACollector.LogTestCase("Email - Get Campaigns List", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailCampaigns",
                TestCaseID        = "GetEmailCampaigns_04",
                Description       = "Status=Pending filter passed directly to repository",
                ExpectedResult    = "Return 200, GetPagedAsync called with Status=Pending",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Status passed to _repo.GetPagedAsync" }
            });
        }

        [Fact]
        public async Task Handle_IncludeDeletedTrue_ShouldPassToRepository()
        {
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), null, null, null, null, null, null, null, true))
                    .ReturnsAsync((new List<EmailJob> { BuildJob("J-DEL", EmailJobStatus.Deleted) }, 1));

            var query = new GetEmailCampaignsQuery { PageNumber = 1, PageSize = 10, IncludeDeleted = true };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            mockRepo.Verify(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), null, null, null, null, null, null, null, true), Times.Once);

            QACollector.LogTestCase("Email - Get Campaigns List", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailCampaigns",
                TestCaseID        = "GetEmailCampaigns_05",
                Description       = "IncludeDeleted=true passed to repository",
                ExpectedResult    = "Return 200, includeDeleted=true in repo call",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.IncludeDeleted = true => passed to GetPagedAsync" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<EmailJobStatus?>(), It.IsAny<UserTargetGroup?>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                    It.IsAny<string?>(), It.IsAny<bool>()))
                    .ThrowsAsync(new Exception("DB down"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(DefaultQuery, CancellationToken.None));

            QACollector.LogTestCase("Email - Get Campaigns List", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailCampaigns",
                TestCaseID        = "GetEmailCampaigns_06",
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
