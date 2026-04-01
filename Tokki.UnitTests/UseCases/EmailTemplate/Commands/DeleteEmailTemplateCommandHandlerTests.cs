using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Xunit;

namespace Tokki.UnitTests.Features.EmailTemplates.Commands
{
    public class DeleteEmailAutoTemplateCommandHandlerTests : EmailTemplateTestBase
    {
        private readonly DeleteEmailAutoTemplateCommandHandler _handler;

        public DeleteEmailAutoTemplateCommandHandlerTests()
        {
            _handler = new DeleteEmailAutoTemplateCommandHandler(_mockRepo.Object);

            // Tránh await null nếu repo methods là Task
            _mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailTemplate>()))
                     .Returns(Task.CompletedTask);

            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TemplateNotFound()
        {
            // Arrange
            var command = new DeleteEmailAutoTemplateCommand
            {
                TemplateId = "tpl-not-found"
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.TemplateId))
                     .ReturnsAsync((EmailTemplate?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailTemplateNotFound.Code);

            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_TemplateAlreadyDeleted()
        {
            // Arrange
            var command = new DeleteEmailAutoTemplateCommand
            {
                TemplateId = "tpl-deleted"
            };

            var existing = new EmailTemplate
            {
                TemplateId = command.TemplateId,
                Status = EmailTemplateStatus.Deleted,
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-1)
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.TemplateId))
                     .ReturnsAsync(existing);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(command.TemplateId);
            result.Message.Should().Be("Template is in Deleted state.");

            // Idempotent: không update DB
            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_TemplateExists_And_NotDeleted()
        {
            // Arrange
            var command = new DeleteEmailAutoTemplateCommand
            {
                TemplateId = "tpl-active"
            };

            var existing = new EmailTemplate
            {
                TemplateId = command.TemplateId,
                Status = EmailTemplateStatus.Draft
                // UpdatedAt là DateTime (non-nullable) => không gán null
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.TemplateId))
                     .ReturnsAsync(existing);

            EmailTemplate? updatedEntity = null;
            _mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailTemplate>()))
                     .Callback<EmailTemplate>(t => updatedEntity = t)
                     .Returns(Task.CompletedTask);

            var nowVn = DateTime.UtcNow.AddHours(7);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(command.TemplateId);
            result.Message.Should().Be("Template deletion successfully!");

            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            updatedEntity.Should().NotBeNull();
            updatedEntity!.Status.Should().Be(EmailTemplateStatus.Deleted);

            updatedEntity.UpdatedAt.Should().BeCloseTo(nowVn, TimeSpan.FromMinutes(2));
        }

        [Fact]
        public async Task Handle_Should_ThrowException_When_UpdateAsyncFails()
        {
            // Arrange
            var command = new DeleteEmailAutoTemplateCommand
            {
                TemplateId = "tpl-error"
            };

            var existing = new EmailTemplate
            {
                TemplateId = command.TemplateId,
                Status = EmailTemplateStatus.Draft
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.TemplateId))
                     .ReturnsAsync(existing);

            _mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailTemplate>()))
                     .ThrowsAsync(new Exception("DB Update Error"));

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                          .Should().ThrowAsync<Exception>()
                          .WithMessage("DB Update Error");

            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ThrowException_When_SaveChangesFails()
        {
            // Arrange
            var command = new DeleteEmailAutoTemplateCommand
            {
                TemplateId = "tpl-save-error"
            };

            var existing = new EmailTemplate
            {
                TemplateId = command.TemplateId,
                Status = EmailTemplateStatus.Draft
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.TemplateId))
                     .ReturnsAsync(existing);

            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("DB Save Error"));

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                          .Should().ThrowAsync<Exception>()
                          .WithMessage("DB Save Error");
        }
    }
}
