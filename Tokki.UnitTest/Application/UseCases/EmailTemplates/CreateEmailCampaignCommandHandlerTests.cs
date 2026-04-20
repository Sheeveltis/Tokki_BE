using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailCampaign;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates
{
    public class CreateEmailCampaignCommandHandlerTests
    {
        private static CreateEmailCampaignByGroupCommandHandler CreateHandler(
            Mock<IEmailJobRepository>? jobRepo = null)
        {
            return new CreateEmailCampaignByGroupCommandHandler(
                (jobRepo ?? new Mock<IEmailJobRepository>()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        private static CreateEmailCampaignByGroupCommand ValidCommand => new()
        {
            CreatedBy      = "ADMIN-001",
            Subject        = "Monthly Newsletter",
            Body           = "<p>Hello Tokki users!</p>",
            TargetGroup    = UserTargetGroup.All,
            ScheduledTime  = DateTimeOffset.UtcNow.AddHours(7).AddDays(1),
            SpecificEmails = null
        };

        [Fact]
        public async Task Handle_ValidCommand_ShouldCreatePendingEmailJob()
        {
            EmailJob? capturedJob = null;
            var mockJobRepo = new Mock<IEmailJobRepository>();
            mockJobRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>()))
                       .Callback<EmailJob>(j => capturedJob = j)
                       .Returns(Task.CompletedTask);
            mockJobRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await CreateHandler(mockJobRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            capturedJob.Should().NotBeNull();
            capturedJob!.Status.Should().Be(EmailJobStatus.Pending);

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup     = "CreateEmailCampaign",
                TestCaseID        = "CreateEmailCampaign_01",
                Description       = "Valid request → EmailJob created with Status=Pending",
                ExpectedResult    = "Return 200, job.Status = Pending",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "job.Status = EmailJobStatus.Pending" }
            });
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldGenerateJobId()
        {
            string? capturedJobId = null;
            var mockJobRepo = new Mock<IEmailJobRepository>();
            mockJobRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>()))
                       .Callback<EmailJob>(j => capturedJobId = j.JobId)
                       .Returns(Task.CompletedTask);
            mockJobRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await CreateHandler(mockJobRepo).Handle(ValidCommand, CancellationToken.None);

            capturedJobId.Should().NotBeNullOrEmpty();
            result.Data.Should().Be(capturedJobId);

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup     = "CreateEmailCampaign",
                TestCaseID        = "CreateEmailCampaign_02",
                Description       = "JobId generated and returned as Result.Data",
                ExpectedResult    = "Result.Data = generated JobId",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "job.JobId = _idGenerator.Generate(15)" }
            });
        }

        [Fact]
        public async Task Handle_WithSpecificEmails_ShouldSerializeToJson()
        {
            string? capturedSpecificEmails = null;
            var mockJobRepo = new Mock<IEmailJobRepository>();
            mockJobRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>()))
                       .Callback<EmailJob>(j => capturedSpecificEmails = j.SpecificEmails)
                       .Returns(Task.CompletedTask);
            mockJobRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var cmd = new CreateEmailCampaignByGroupCommand
            {
                CreatedBy      = "ADMIN-001",
                Subject        = "Specific Email",
                Body           = "<p>Test</p>",
                TargetGroup    = UserTargetGroup.All,
                SpecificEmails = new List<string> { "a@b.com", "c@d.com" }
            };

            var result = await CreateHandler(mockJobRepo).Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            capturedSpecificEmails.Should().NotBeNullOrEmpty();
            capturedSpecificEmails.Should().Contain("a@b.com");

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup     = "CreateEmailCampaign",
                TestCaseID        = "CreateEmailCampaign_03",
                Description       = "SpecificEmails list serialized to JSON and stored",
                ExpectedResult    = "job.SpecificEmails contains serialized JSON of email list",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.SpecificEmails.Any() => JsonSerializer.Serialize" }
            });
        }

        [Fact]
        public async Task Handle_NoSpecificEmails_ShouldSetSpecificEmailsToNull()
        {
            string? capturedSpecificEmails = "PLACEHOLDER";
            var mockJobRepo = new Mock<IEmailJobRepository>();
            mockJobRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>()))
                       .Callback<EmailJob>(j => capturedSpecificEmails = j.SpecificEmails)
                       .Returns(Task.CompletedTask);
            mockJobRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await CreateHandler(mockJobRepo).Handle(ValidCommand, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            capturedSpecificEmails.Should().BeNull();

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup     = "CreateEmailCampaign",
                TestCaseID        = "CreateEmailCampaign_04",
                Description       = "SpecificEmails is null → job.SpecificEmails = null",
                ExpectedResult    = "job.SpecificEmails = null",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!request.SpecificEmails.Any() => specificEmailsJson = null" }
            });
        }

        [Fact]
        public async Task Handle_NoScheduledTime_ShouldUseCurrentVNTime()
        {
            DateTime? capturedScheduledTime = null;
            var before = DateTime.UtcNow.AddHours(7).AddMinutes(-1);

            var mockJobRepo = new Mock<IEmailJobRepository>();
            mockJobRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>()))
                       .Callback<EmailJob>(j => capturedScheduledTime = j.ScheduledTime)
                       .Returns(Task.CompletedTask);
            mockJobRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var cmd = new CreateEmailCampaignByGroupCommand
            {
                CreatedBy      = "ADMIN-001",
                Subject        = "Immediate",
                Body           = "<p>Now</p>",
                TargetGroup    = UserTargetGroup.All,
                ScheduledTime  = null   // no schedule → use now
            };

            await CreateHandler(mockJobRepo).Handle(cmd, CancellationToken.None);

            capturedScheduledTime.Should().BeAfter(before);

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup     = "CreateEmailCampaign",
                TestCaseID        = "CreateEmailCampaign_05",
                Description       = "ScheduledTime = null → defaults to current VN time",
                ExpectedResult    = "job.ScheduledTime ≈ DateTime.UtcNow.AddHours(7)",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "request.ScheduledTime == null => sendTime = nowVn" }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockJobRepo = new Mock<IEmailJobRepository>();
            mockJobRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>())).ThrowsAsync(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mockJobRepo).Handle(ValidCommand, CancellationToken.None));

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup     = "CreateEmailCampaign",
                TestCaseID        = "CreateEmailCampaign_06",
                Description       = "Repository AddAsync throws → exception propagates",
                ExpectedResult    = "Throws Exception",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AddAsync throws" }
            });
        }
    }
}
