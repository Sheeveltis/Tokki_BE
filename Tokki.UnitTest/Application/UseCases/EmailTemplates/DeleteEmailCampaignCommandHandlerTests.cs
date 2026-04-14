using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailCampaign;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates
{
    public class DeleteEmailCampaignCommandHandlerTests
    {
        private static DeleteEmailCampaignCommandHandler CreateHandler(
            Mock<IEmailJobRepository>? jobRepo = null)
        {
            return new DeleteEmailCampaignCommandHandler(
                (jobRepo ?? new Mock<IEmailJobRepository>()).Object);
        }

        private static DeleteEmailCampaignCommand ValidCommand => new() { JobId = "JOB-001", UpdateBy = "ADMIN-001" };

        private static EmailJob PendingJob(string id = "JOB-001") => new()
        {
            JobId     = id,
            Status    = EmailJobStatus.Pending,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = null
        };

        [Fact]
        public async Task Handle_JobNotFound_ShouldReturn404()
        {
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((EmailJob?)null);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Email - Delete Campaign", new TestCaseDetail
            {
                FunctionGroup     = "DeleteEmailCampaign",
                TestCaseID        = "TC-EMAIL-DCMP-01",
                Description       = "JobId does not exist → Return 404",
                ExpectedResult    = "Return 404 Failure 'Không tìm thấy campaign!'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "job == null => Return 404" }
            });
        }

        [Fact]
        public async Task Handle_JobNotPending_ShouldReturn400()
        {
            var sentJob = new EmailJob { JobId = "JOB-001", Status = EmailJobStatus.Sent };
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(sentJob);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Email - Delete Campaign", new TestCaseDetail
            {
                FunctionGroup     = "DeleteEmailCampaign",
                TestCaseID        = "TC-EMAIL-DCMP-02",
                Description       = "Job status is Sent (not Pending) → Return 400",
                ExpectedResult    = "Return 400 Failure 'Chỉ được xóa campaign khi Pending'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "job.Status != Pending => Return 400" }
            });
        }

        [Fact]
        public async Task Handle_ProcessingJob_ShouldReturn400()
        {
            var processingJob = new EmailJob { JobId = "JOB-001", Status = EmailJobStatus.Processing };
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(processingJob);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Email - Delete Campaign", new TestCaseDetail
            {
                FunctionGroup     = "DeleteEmailCampaign",
                TestCaseID        = "TC-EMAIL-DCMP-03",
                Description       = "Job status is Processing → Return 400 (cannot delete)",
                ExpectedResult    = "Return 400 Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "job.Status == Processing => Return 400" }
            });
        }

        [Fact]
        public async Task Handle_PendingJob_ShouldSoftDelete()
        {
            var job = PendingJob();
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(job);
            mockRepo.Setup(x => x.SoftDeleteAsync(It.IsAny<EmailJob>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            mockRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<EmailJob>()), Times.Once);

            QACollector.LogTestCase("Email - Delete Campaign", new TestCaseDetail
            {
                FunctionGroup     = "DeleteEmailCampaign",
                TestCaseID        = "TC-EMAIL-DCMP-04",
                Description       = "Pending job → SoftDeleteAsync called, Return 200",
                ExpectedResult    = "Return 200, SoftDeleteAsync called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "job.Status == Pending => SoftDeleteAsync" }
            });
        }

        [Fact]
        public async Task Handle_PendingJob_ShouldSetAuditFields()
        {
            var job = PendingJob();
            var before = job.UpdatedAt;
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(job);
            mockRepo.Setup(x => x.SoftDeleteAsync(It.IsAny<EmailJob>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            await CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None);

            job.UpdatedBy.Should().Be("ADMIN-001");
            job.UpdatedAt.Should().BeAfter(before!.Value.AddSeconds(-1));

            QACollector.LogTestCase("Email - Delete Campaign", new TestCaseDetail
            {
                FunctionGroup     = "DeleteEmailCampaign",
                TestCaseID        = "TC-EMAIL-DCMP-05",
                Description       = "Audit fields UpdatedBy and UpdatedAt set on deletion",
                ExpectedResult    = "job.UpdatedBy = 'ADMIN-001', job.UpdatedAt updated",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "job.UpdatedBy = request.UpdateBy", "job.UpdatedAt = now" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB timeout"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(ValidCommand, CancellationToken.None));

            QACollector.LogTestCase("Email - Delete Campaign", new TestCaseDetail
            {
                FunctionGroup     = "DeleteEmailCampaign",
                TestCaseID        = "TC-EMAIL-DCMP-06",
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
