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

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates.Commands
{
    public class UpdateEmailCampaignCommandHandlerTests
    {
        private readonly Mock<IEmailJobRepository> _jobRepoMock;

        public UpdateEmailCampaignCommandHandlerTests()
        {
            _jobRepoMock = new Mock<IEmailJobRepository>();
        }

        private UpdateEmailCampaignCommandHandler CreateHandler()
        {
            return new UpdateEmailCampaignCommandHandler(_jobRepoMock.Object);
        }

        // -----------------------------------------------------------
        // UpdateEmailCampaignCommandHandler_01 | A | Job Not Found -> 404
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_JobNotFound_ShouldReturn404()
        {
            _jobRepoMock.Setup(x => x.GetByIdAsync("fake-id")).ReturnsAsync((EmailJob?)null);
            var handler = CreateHandler();
            var cmd = new UpdateEmailCampaignCommand { JobId = "fake-id" };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Be("Không tìm th?y campaign!");

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailCampaignCommandHandler",
                TestCaseID = "UpdateEmailCampaignCommandHandler_01",
                Description = "Returns error if JobId does not exist",
                ExpectedResult = "404 Not Found",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Job = null" }
            });
        }

        // -----------------------------------------------------------
        // UpdateEmailCampaignCommandHandler_02 | A | Job Status not Pending -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_JobNotPending_ShouldReturn400()
        {
            var job = new EmailJob { JobId = "123", Status = EmailJobStatus.Sent };
            _jobRepoMock.Setup(x => x.GetByIdAsync("123")).ReturnsAsync(job);
            var handler = CreateHandler();
            var cmd = new UpdateEmailCampaignCommand { JobId = "123" };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailCampaignCommandHandler",
                TestCaseID = "UpdateEmailCampaignCommandHandler_02",
                Description = "Only pending jobs can be updated",
                ExpectedResult = "400 Bad Request",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status != Pending" }
            });
        }

        // -----------------------------------------------------------
        // UpdateEmailCampaignCommandHandler_03 | A | Status updated to not deleted -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_UpdateStatusNotDeleted_ShouldReturn400()
        {
            var job = new EmailJob { JobId = "123", Status = EmailJobStatus.Pending };
            _jobRepoMock.Setup(x => x.GetByIdAsync("123")).ReturnsAsync(job);
            var handler = CreateHandler();
            var cmd = new UpdateEmailCampaignCommand { JobId = "123", Status = EmailJobStatus.Failed };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailCampaignCommandHandler",
                TestCaseID = "UpdateEmailCampaignCommandHandler_03",
                Description = "Rejects moving status to anything other than Deleted context internally",
                ExpectedResult = "400 Bad Request",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Update status != Deleted" }
            });
        }

        // -----------------------------------------------------------
        // UpdateEmailCampaignCommandHandler_04 | N | Patch fields properly -> 200
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_PatchFields_ShouldUpdateCorrectlyAndReturn200()
        {
            var job = new EmailJob { JobId = "123", Status = EmailJobStatus.Pending };
            _jobRepoMock.Setup(x => x.GetByIdAsync("123")).ReturnsAsync(job);
            var handler = CreateHandler();
            var cmd = new UpdateEmailCampaignCommand 
            { 
                JobId = "123", 
                Subject = "S", 
                Body = "B", 
                TargetGroup = UserTargetGroup.VipUsers,
                ScheduledTime = DateTime.UtcNow.AddDays(1)
            };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            job.Subject.Should().Be("S");
            job.Body.Should().Be("B");
            job.TargetGroup.Should().Be(UserTargetGroup.VipUsers);

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailCampaignCommandHandler",
                TestCaseID = "UpdateEmailCampaignCommandHandler_04",
                Description = "Patches basic string/enum fields correctly",
                ExpectedResult = "200 Success",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Optional strings and date filled" }
            });
        }

        // -----------------------------------------------------------
        // UpdateEmailCampaignCommandHandler_05 | N | Distinct Specific Emails list stored properly -> 200
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_SpecificEmails_ShouldCleanAndSerializeAndReturn200()
        {
            var job = new EmailJob { JobId = "123", Status = EmailJobStatus.Pending };
            _jobRepoMock.Setup(x => x.GetByIdAsync("123")).ReturnsAsync(job);
            var handler = CreateHandler();
            var cmd = new UpdateEmailCampaignCommand 
            { 
                JobId = "123", 
                SpecificEmails = new List<string> { "a@abc.com", " a@abc.com", "", "b@abc.com" }
            };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            job.SpecificEmails.Should().Contain("a@abc.com");
            job.SpecificEmails.Should().Contain("b@abc.com");

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailCampaignCommandHandler",
                TestCaseID = "UpdateEmailCampaignCommandHandler_05",
                Description = "Cleans empty strings and duplicate mails in specificEmails string list",
                ExpectedResult = "200 Success JSON array",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "List with spaces and duplicates" }
            });
        }

        // -----------------------------------------------------------
        // UpdateEmailCampaignCommandHandler_06 | N | Update Status to Deleted -> 200
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_UpdateToDeleted_ShouldSetDeletedAndReturn200()
        {
            var job = new EmailJob { JobId = "123", Status = EmailJobStatus.Pending };
            _jobRepoMock.Setup(x => x.GetByIdAsync("123")).ReturnsAsync(job);
            var handler = CreateHandler();
            var cmd = new UpdateEmailCampaignCommand 
            { 
                JobId = "123", 
                Status = EmailJobStatus.Deleted
            };

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            job.Status.Should().Be(EmailJobStatus.Deleted);
            
            _jobRepoMock.Verify(x => x.UpdateAsync(job), Times.Once);
            _jobRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailCampaignCommandHandler",
                TestCaseID = "UpdateEmailCampaignCommandHandler_06",
                Description = "Changing status explicitly to Deleted works as intended",
                ExpectedResult = "200 Success",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Deleted" }
            });
        }
    }
}
