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
    public class CreateEmailCampaignByGroupCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static CreateEmailCampaignByGroupCommandHandler CreateHandler(Mock<IEmailJobRepository>? repo = null)
        {
            var mockRepo = repo ?? BuildDefaultRepo();
            return new CreateEmailCampaignByGroupCommandHandler(mockRepo.Object, MockIdGeneratorService.GetMock().Object);
        }

        private static Mock<IEmailJobRepository> BuildDefaultRepo()
        {
            var mock = new Mock<IEmailJobRepository>();
            mock.Setup(x => x.AddAsync(It.IsAny<EmailJob>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return mock;
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ECC-01 | N | Valid campaign → 200 Success with JobId
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCampaign_ShouldReturn200WithJobId()
        {
            // Arrange
            var command = new CreateEmailCampaignByGroupCommand
            {
                Subject     = "Notification",
                Body        = "Content",
                TargetGroup = UserTargetGroup.All,
                CreatedBy   = "ADMIN-001"
            };

            var mockRepo = BuildDefaultRepo();

            // Act
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNullOrEmpty();
            mockRepo.Verify(x => x.AddAsync(It.IsAny<EmailJob>()), Times.Once);

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup     = "Create Email Campaign",
                TestCaseID        = "TC-ECC-01",
                Description       = "Create a valid email campaign targeting all users",
                ExpectedResult    = "Return 200 with generated JobId, AddAsync called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid Subject/Body/TargetGroup", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ECC-02 | N | ScheduledTime is persisted correctly
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WithScheduledTime_ShouldPersistSchedule()
        {
            // Arrange
            var scheduledTime = DateTimeOffset.UtcNow.AddHours(7).AddDays(1);
            EmailJob? capturedJob = null;

            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>()))
                    .Callback<EmailJob>(j => capturedJob = j)
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var command = new CreateEmailCampaignByGroupCommand
            {
                Subject       = "Scheduled",
                Body          = "Body",
                TargetGroup   = UserTargetGroup.All,
                CreatedBy     = "ADMIN-001",
                ScheduledTime = scheduledTime
            };

            // Act
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedJob.Should().NotBeNull();
            capturedJob!.ScheduledTime.Should().BeCloseTo(
                scheduledTime.ToOffset(TimeSpan.FromHours(7)).DateTime,
                TimeSpan.FromMinutes(1));

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup     = "Create Email Campaign",
                TestCaseID        = "TC-ECC-02",
                Description       = "Create campaign with a future ScheduledTime — persisted correctly",
                ExpectedResult    = "Return 200, EmailJob.ScheduledTime ≈ command.ScheduledTime",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ScheduledTime = tomorrow", "Captured via Callback" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ECC-03 | N | CreatedBy is stored in job entity
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCampaign_CreatedByStoredOnJob()
        {
            // Arrange
            EmailJob? capturedJob = null;
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>()))
                    .Callback<EmailJob>(j => capturedJob = j)
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var command = new CreateEmailCampaignByGroupCommand
            {
                Subject     = "Test",
                Body        = "Body",
                TargetGroup = UserTargetGroup.All,
                CreatedBy   = "ADMIN-XYZ"
            };

            // Act
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            capturedJob!.CreatedBy.Should().Be("ADMIN-XYZ");

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup     = "Create Email Campaign",
                TestCaseID        = "TC-ECC-03",
                Description       = "Verify CreatedBy field is forwarded to the stored EmailJob entity",
                ExpectedResult    = "EmailJob.CreatedBy = 'ADMIN-XYZ'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CreatedBy forwarded to entity", "Captured via Callback" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ECC-04 | N | TargetGroup is stored on entity
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCampaign_TargetGroupStoredOnJob()
        {
            // Arrange
            EmailJob? capturedJob = null;
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>()))
                    .Callback<EmailJob>(j => capturedJob = j)
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var command = new CreateEmailCampaignByGroupCommand
            {
                Subject     = "VIP",
                Body        = "Body",
                TargetGroup = UserTargetGroup.VipUsers,
                CreatedBy   = "ADMIN-001"
            };

            // Act
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            // Assert
            capturedJob!.TargetGroup.Should().Be(UserTargetGroup.VipUsers);

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup     = "Create Email Campaign",
                TestCaseID        = "TC-ECC-04",
                Description       = "Verify TargetGroup is correctly persisted on the stored EmailJob entity",
                ExpectedResult    = "EmailJob.TargetGroup = Vip",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TargetGroup = Vip", "Captured via Callback" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ECC-05 | A | AddAsync throws → exception propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AddAsyncThrows_ShouldPropagateException()
        {
            // Arrange
            var mockRepo = new Mock<IEmailJobRepository>();
            mockRepo.Setup(x => x.AddAsync(It.IsAny<EmailJob>())).ThrowsAsync(new Exception("DB Error"));

            var command = new CreateEmailCampaignByGroupCommand
            {
                Subject     = "Test",
                Body        = "Body",
                TargetGroup = UserTargetGroup.All,
                CreatedBy   = "ADMIN-001"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo).Handle(command, CancellationToken.None));

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup     = "Create Email Campaign",
                TestCaseID        = "TC-ECC-05",
                Description       = "Repository throws exception during job creation",
                ExpectedResult    = "Exception propagates to global middleware",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AddAsync throws Exception", "No try/catch in handler" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-ECC-06 | B | Subject is empty string → still creates job
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptySubject_ShouldStillCreateJob()
        {
            // Arrange — handler does not validate, just persists whatever is provided
            var command = new CreateEmailCampaignByGroupCommand
            {
                Subject     = "",
                Body        = "Body",
                TargetGroup = UserTargetGroup.All,
                CreatedBy   = "ADMIN-001"
            };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            QACollector.LogTestCase("Email - Create Campaign", new TestCaseDetail
            {
                FunctionGroup     = "Create Email Campaign",
                TestCaseID        = "TC-ECC-06",
                Description       = "Boundary: create campaign with empty Subject string — handler persists without validation",
                ExpectedResult    = "Return 200 Success; Subject stored as empty string",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Subject = empty string", "No validation in handler" }
            });
        }
    }
}