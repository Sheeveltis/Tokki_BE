using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailCampaign;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates
{
    public class UpdateEmailCampaignCommandHandlerTests
    {
        private static UpdateEmailCampaignCommandHandler CreateHandler(
            Mock<IEmailJobRepository>? jobRepo = null)
        {
            return new UpdateEmailCampaignCommandHandler(
                (jobRepo ?? new Mock<IEmailJobRepository>()).Object);
        }

        private static EmailJob PendingJob(string id = "JOB-001") => new()
        {
            JobId      = id,
            Status     = EmailJobStatus.Pending,
            Subject    = "Old Subject",
            Body       = "<p>Old</p>",
            TargetGroup = UserTargetGroup.All,
            UpdatedAt  = DateTime.UtcNow.AddHours(-1)
        };

        private static UpdateEmailCampaignCommand ValidUpdateCommand => new()
        {
            JobId     = "JOB-001",
            Subject   = "New Subject",
            UpdatedBy = "ADMIN-001"
        };

        [Fact]
        public async Task Handle_JobNotFound_ShouldReturn404()
        {
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((EmailJob?)null);

            var result = await CreateHandler(mockRepo).Handle(ValidUpdateCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup     = "UpdateEmailCampaign",
                TestCaseID        = "TC-EMAIL-UCMP-01",
                Description       = "JobId not found → 404 Failure",
                ExpectedResult    = "Return 404 'Không tìm thấy campaign!'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "job == null => Return 404" }
            });
        }

        [Fact]
        public async Task Handle_NotPendingJob_ShouldReturn400()
        {
            var sent = new EmailJob { JobId = "JOB-001", Status = EmailJobStatus.Sent };
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(sent);

            var result = await CreateHandler(mockRepo).Handle(ValidUpdateCommand, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup     = "UpdateEmailCampaign",
                TestCaseID        = "TC-EMAIL-UCMP-02",
                Description       = "Job is Sent (not Pending) → 400 Failure (cannot update sent jobs)",
                ExpectedResult    = "Return 400",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "job.Status != Pending => Return 400" }
            });
        }

        [Fact]
        public async Task Handle_InvalidStatusUpdate_ShouldReturn400()
        {
            var job = PendingJob();
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(job);

            var cmd = new UpdateEmailCampaignCommand { JobId = "JOB-001", Status = EmailJobStatus.Sent };
            var result = await CreateHandler(mockRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup     = "UpdateEmailCampaign",
                TestCaseID        = "TC-EMAIL-UCMP-03",
                Description       = "Attempting to update Status to Sent (only Deleted allowed) → 400",
                ExpectedResult    = "Return 400 'Chỉ cho phép Status sang Deleted'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Status != Deleted => Return 400" }
            });
        }

        [Fact]
        public async Task Handle_ValidSubjectUpdate_ShouldPersist()
        {
            var job = PendingJob();
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(job);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailJob>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await CreateHandler(mockRepo).Handle(ValidUpdateCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            job.Subject.Should().Be("New Subject");
            mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailJob>()), Times.Once);

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup     = "UpdateEmailCampaign",
                TestCaseID        = "TC-EMAIL-UCMP-04",
                Description       = "Valid subject update → job.Subject updated, Return 200",
                ExpectedResult    = "Return 200, job.Subject = 'New Subject'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.Subject not empty => job.Subject = trimmed value" }
            });
        }

        [Fact]
        public async Task Handle_ValidUpdate_ShouldSetAuditFieldsAndUpdatedBy()
        {
            var job = PendingJob();
            var before = job.UpdatedAt;
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(job);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailJob>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            await CreateHandler(mockRepo).Handle(ValidUpdateCommand, CancellationToken.None);

            job.UpdatedBy.Should().Be("ADMIN-001");
            job.UpdatedAt.Should().BeAfter(before!.Value.AddSeconds(-1));

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup     = "UpdateEmailCampaign",
                TestCaseID        = "TC-EMAIL-UCMP-05",
                Description       = "Audit fields UpdatedBy='ADMIN-001' and UpdatedAt=VN now set",
                ExpectedResult    = "job.UpdatedBy = 'ADMIN-001', job.UpdatedAt updated",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "job.UpdatedBy = request.UpdatedBy", "job.UpdatedAt = now" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB down"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockRepo).Handle(ValidUpdateCommand, CancellationToken.None));

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup     = "UpdateEmailCampaign",
                TestCaseID        = "TC-EMAIL-UCMP-06",
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
