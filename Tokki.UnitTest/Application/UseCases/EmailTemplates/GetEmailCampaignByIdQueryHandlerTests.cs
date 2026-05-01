using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailCampaignById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates
{
    public class GetEmailCampaignByIdQueryHandlerTests
    {
        private static GetEmailCampaignByIdQueryHandler CreateHandler(
            Mock<IEmailJobRepository>? jobRepo = null)
        {
            return new GetEmailCampaignByIdQueryHandler(
                (jobRepo ?? new Mock<IEmailJobRepository>()).Object);
        }

        private static EmailJob BuildJob(string id = "JOB-001", EmailJobStatus status = EmailJobStatus.Pending) => new()
        {
            JobId       = id,
            Subject     = "Newsletter",
            Status      = status,
            TargetGroup = UserTargetGroup.All,
            ScheduledTime = DateTime.UtcNow.AddDays(1)
        };

        [Fact]
        public async Task Handle_JobNotFound_ShouldReturn404()
        {
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((EmailJob?)null);

            var result = await CreateHandler(mockRepo).Handle(
                new GetEmailCampaignByIdQuery { JobId = "INVALID" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Contain("Không tìm thấy");

            QACollector.LogTestCase("Email - Get Campaign By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailCampaignById",
                TestCaseID        = "GetEmailCampaignById_01",
                Description       = "JobId does not exist → Return 404 'Không tìm thấy campaign!'",
                ExpectedResult    = "Return 404 Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "job == null => Return 404" }
            });
        }

        [Fact]
        public async Task Handle_PendingJob_ShouldReturn200WithJobEntity()
        {
            var job = BuildJob("JOB-001", EmailJobStatus.Pending);
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("JOB-001")).ReturnsAsync(job);

            var result = await CreateHandler(mockRepo).Handle(
                new GetEmailCampaignByIdQuery { JobId = "JOB-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.JobId.Should().Be("JOB-001");
            result.Data.Status.Should().Be(EmailJobStatus.Pending);

            QACollector.LogTestCase("Email - Get Campaign By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailCampaignById",
                TestCaseID        = "GetEmailCampaignById_02",
                Description       = "Pending job found → Return 200, Data = EmailJob entity",
                ExpectedResult    = "Return 200, Data.JobId = 'JOB-001', Status = Pending",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "job != null => Return 200" }
            });
        }

        [Fact]
        public async Task Handle_SentJob_ShouldReturn200WithJobEntity()
        {
            var job = BuildJob("JOB-002", EmailJobStatus.Sent);
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("JOB-002")).ReturnsAsync(job);

            var result = await CreateHandler(mockRepo).Handle(
                new GetEmailCampaignByIdQuery { JobId = "JOB-002" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Status.Should().Be(EmailJobStatus.Sent);

            QACollector.LogTestCase("Email - Get Campaign By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailCampaignById",
                TestCaseID        = "GetEmailCampaignById_03",
                Description       = "Sent job found → Return 200 (get works for all statuses)",
                ExpectedResult    = "Return 200, Data.Status = Sent",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "job.Status == Sent => Return 200" }
            });
        }

        [Fact]
        public async Task Handle_DeletedJob_ShouldReturn200_NoFiltering()
        {
            var deleted = BuildJob("JOB-DEL", EmailJobStatus.Deleted);
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("JOB-DEL")).ReturnsAsync(deleted);

            var result = await CreateHandler(mockRepo).Handle(
                new GetEmailCampaignByIdQuery { JobId = "JOB-DEL" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Status.Should().Be(EmailJobStatus.Deleted);

            QACollector.LogTestCase("Email - Get Campaign By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailCampaignById",
                TestCaseID        = "GetEmailCampaignById_04",
                Description       = "Deleted job returned (no filtering by status in GetById) → Return 200",
                ExpectedResult    = "Return 200, Data.Status = Deleted",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetById doesn't filter by status" }
            });
        }

        [Fact]
        public async Task Handle_ValidJob_ShouldMatchJobId()
        {
            var job = BuildJob("JOB-MATCH");
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync("JOB-MATCH")).ReturnsAsync(job);

            var result = await CreateHandler(mockRepo).Handle(
                new GetEmailCampaignByIdQuery { JobId = "JOB-MATCH" }, CancellationToken.None);

            result.Data!.JobId.Should().Be("JOB-MATCH");

            QACollector.LogTestCase("Email - Get Campaign By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailCampaignById",
                TestCaseID        = "GetEmailCampaignById_05",
                Description       = "JobId matches in result DTO",
                ExpectedResult    = "Result.Data.JobId = 'JOB-MATCH'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "result.Data = job entity" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB down"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(
                    new GetEmailCampaignByIdQuery { JobId = "JOB-001" }, CancellationToken.None));

            QACollector.LogTestCase("Email - Get Campaign By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetEmailCampaignById",
                TestCaseID        = "GetEmailCampaignById_06",
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
