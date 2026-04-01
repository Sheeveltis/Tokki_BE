using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailCampaign;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Xunit;

namespace Tokki.UnitTests.Features.EmailCampaigns.Commands
{
    public class UpdateEmailCampaignCommandHandlerTests
    {
        private readonly Mock<IEmailJobRepository> _mockRepo;
        private readonly UpdateEmailCampaignCommandHandler _handler;

        public UpdateEmailCampaignCommandHandlerTests()
        {
            _mockRepo = new Mock<IEmailJobRepository>();
            _handler = new UpdateEmailCampaignCommandHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_JobNotFound()
        {
            // Arrange
            var command = new UpdateEmailCampaignCommand
            {
                JobId = "job-not-found",
                Subject = "New Subject"
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.JobId))
                     .ReturnsAsync((EmailJob?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Be("Campaign not found!");

            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailJob>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_StatusNotPending()
        {
            // Arrange
            var command = new UpdateEmailCampaignCommand
            {
                JobId = "job-01",
                Subject = "New Subject"
            };

            var job = new EmailJob
            {
                JobId = command.JobId,
                Status = EmailJobStatus.Sent, // != Pending
                Subject = "Old",
                Body = "Old",
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-1),
                UpdatedBy = "seed"
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.JobId))
                     .ReturnsAsync(job);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("Campaigns can only be updated when the status is Pending (not sent).");

            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailJob>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_PatchFields_And_SetAudit_When_Pending()
        {
            // Arrange
            var command = new UpdateEmailCampaignCommand
            {
                JobId = "job-pending",
                Subject = "  New Subject  ",
                Body = "New Body",
                TargetGroup = UserTargetGroup.VipUsers,
                SpecificEmails = new List<string>
                {
                    " a@tokki.vn ",
                    "A@tokki.vn",
                    "b@tokki.vn",
                    "  ",
                    ""
                },
                ScheduledTime = DateTime.UtcNow.AddHours(7).AddHours(2),
                UpdatedBy = "admin-01"
            };

            var job = new EmailJob
            {
                JobId = command.JobId,
                Status = EmailJobStatus.Pending,
                Subject = "Old Subject",
                Body = "Old Body",
                TargetGroup = UserTargetGroup.All,
                SpecificEmails = null,
                ScheduledTime = DateTime.UtcNow.AddHours(7).AddHours(1),
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-3),
                UpdatedBy = "seed"
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.JobId))
                     .ReturnsAsync(job);

            EmailJob? updatedEntity = null;
            _mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailJob>()))
                     .Callback<EmailJob>(j => updatedEntity = j)
                     .Returns(Task.CompletedTask);

            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

            var nowVn = DateTime.UtcNow.AddHours(7);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(command.JobId);
            result.Message.Should().Be("Successfully updated campaign!");

            _mockRepo.Verify(x => x.GetByIdAsync(command.JobId), Times.Once);
            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailJob>()), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            updatedEntity.Should().NotBeNull();

            // Patch fields
            updatedEntity!.Subject.Should().Be("New Subject"); // trimmed
            updatedEntity.Body.Should().Be("New Body");
            updatedEntity.TargetGroup.Should().Be(UserTargetGroup.VipUsers);
            updatedEntity.ScheduledTime.Should().Be(command.ScheduledTime!.Value);

            // SpecificEmails JSON: cleaned + distinct ignore case => ["a@tokki.vn","b@tokki.vn"] (order theo code)
            updatedEntity.SpecificEmails.Should().NotBeNull();
            var list = JsonSerializer.Deserialize<List<string>>(updatedEntity.SpecificEmails!);
            list.Should().BeEquivalentTo(new List<string> { "a@tokki.vn", "b@tokki.vn" }, opt => opt.WithStrictOrdering());

            // Audit
            updatedEntity.UpdatedBy.Should().Be("admin-01");
            updatedEntity.UpdatedAt.Should().BeCloseTo(nowVn, TimeSpan.FromMinutes(2));
        }

        [Fact]
        public async Task Handle_Should_SetUpdatedBy_System_When_UpdatedByIsNullOrWhitespace()
        {
            // Arrange
            var command = new UpdateEmailCampaignCommand
            {
                JobId = "job-pending",
                Subject = "New Subject",
                UpdatedBy = "   " // whitespace => system
            };

            var job = new EmailJob
            {
                JobId = command.JobId,
                Status = EmailJobStatus.Pending,
                Subject = "Old Subject",
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-3),
                UpdatedBy = "seed"
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.JobId))
                     .ReturnsAsync(job);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            job.UpdatedBy.Should().Be("system");
            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailJob>()), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        [Fact]
        public async Task Handle_Should_ReturnFailure_When_StatusIsProvided_But_NotDeleted()
        {
            // Arrange
            var command = new UpdateEmailCampaignCommand
            {
                JobId = "job-pending",
                Status = EmailJobStatus.Sent // ✅ dùng status hợp lệ nhưng KHÔNG phải Deleted
            };

            var job = new EmailJob
            {
                JobId = command.JobId,
                Status = EmailJobStatus.Pending,
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-1)
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.JobId))
                     .ReturnsAsync(job);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("Only status updates to Deleted are allowed.");

            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailJob>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_UpdateStatusToDeleted_When_StatusDeleted()
        {
            // Arrange
            var command = new UpdateEmailCampaignCommand
            {
                JobId = "job-pending",
                Status = EmailJobStatus.Deleted,
                UpdatedBy = "admin-01"
            };

            var job = new EmailJob
            {
                JobId = command.JobId,
                Status = EmailJobStatus.Pending
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.JobId))
                     .ReturnsAsync(job);

            EmailJob? updatedEntity = null;
            _mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailJob>()))
                     .Callback<EmailJob>(j => updatedEntity = j)
                     .Returns(Task.CompletedTask);

            var nowVn = DateTime.UtcNow.AddHours(7);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(command.JobId);
            result.Message.Should().Be("Successfully updated campaign!");

            updatedEntity.Should().NotBeNull();
            updatedEntity!.Status.Should().Be(EmailJobStatus.Deleted);
            updatedEntity.UpdatedBy.Should().Be("admin-01");
            updatedEntity.UpdatedAt.Should().BeCloseTo(nowVn, TimeSpan.FromMinutes(2));

            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailJob>()), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_SetSpecificEmailsNull_When_ListProvidedButAllEmpty()
        {
            // Arrange
            var command = new UpdateEmailCampaignCommand
            {
                JobId = "job-pending",
                SpecificEmails = new List<string> { " ", "", "   " }
            };

            var job = new EmailJob
            {
                JobId = command.JobId,
                Status = EmailJobStatus.Pending,
                SpecificEmails = JsonSerializer.Serialize(new List<string> { "a@tokki.vn" })
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.JobId))
                     .ReturnsAsync(job);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            job.SpecificEmails.Should().BeNull();

            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailJob>()), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
