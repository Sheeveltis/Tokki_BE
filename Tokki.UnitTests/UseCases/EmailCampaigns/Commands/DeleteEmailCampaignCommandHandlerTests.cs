using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailCampaign;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Xunit;

namespace Tokki.UnitTests.Features.EmailCampaigns.Commands
{
    public class DeleteEmailCampaignCommandHandlerTests
    {
        private readonly Mock<IEmailJobRepository> _mockRepo;
        private readonly DeleteEmailCampaignCommandHandler _handler;

        public DeleteEmailCampaignCommandHandlerTests()
        {
            _mockRepo = new Mock<IEmailJobRepository>();
            _handler = new DeleteEmailCampaignCommandHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_JobNotFound()
        {
            // Arrange
            var command = new DeleteEmailCampaignCommand
            {
                JobId = "job-not-exist",
                UpdateBy = "admin-01"
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.JobId))
                     .ReturnsAsync((EmailJob?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Be("Campaign not found!");

            _mockRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<EmailJob>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_StatusNotPending()
        {
            // Arrange
            var command = new DeleteEmailCampaignCommand
            {
                JobId = "job-01",
                UpdateBy = "admin-01"
            };

            var job = new EmailJob
            {
                JobId = command.JobId,
                Status = EmailJobStatus.Sent, // != Pending
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-1),
                UpdatedBy = null
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.JobId))
                     .ReturnsAsync(job);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("Campaigns can only be deleted when the status is Pending (not sent).");

            _mockRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<EmailJob>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_And_SoftDelete_When_StatusPending()
        {
            // Arrange
            var command = new DeleteEmailCampaignCommand
            {
                JobId = "job-pending",
                UpdateBy = "admin-01"
            };

            var job = new EmailJob
            {
                JobId = command.JobId,
                Status = EmailJobStatus.Pending,
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-2),
                UpdatedBy = null
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.JobId))
                     .ReturnsAsync(job);

            EmailJob? softDeletedJob = null;
            _mockRepo.Setup(x => x.SoftDeleteAsync(It.IsAny<EmailJob>()))
                     .Callback<EmailJob>(j => softDeletedJob = j)
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
            result.Message.Should().Be("Delete campaign (soft delete) successfully!");

            _mockRepo.Verify(x => x.GetByIdAsync(command.JobId), Times.Once);
            _mockRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<EmailJob>()), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            softDeletedJob.Should().NotBeNull();
            softDeletedJob!.UpdatedBy.Should().Be(command.UpdateBy);
            softDeletedJob.UpdatedAt.Should().BeCloseTo(nowVn, TimeSpan.FromMinutes(2));
        }
    }
}
