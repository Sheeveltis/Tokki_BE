using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Xunit;

namespace Tokki.UnitTests.Features.EmailTemplates.Commands
{
    public class UpdateEmailAutoTemplateCommandHandlerTests : EmailTemplateTestBase
    {
        private readonly UpdateEmailAutoTemplateCommandHandler _handler;

        public UpdateEmailAutoTemplateCommandHandlerTests()
        {
            _handler = new UpdateEmailAutoTemplateCommandHandler(_mockRepo.Object);

            _mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailTemplate>()))
                     .Returns(Task.CompletedTask);

            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TemplateNotFound()
        {
            // Arrange
            var command = new UpdateEmailAutoTemplateCommand
            {
                TemplateId = "tpl-not-found",
                Subject = "New Subject"
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
        public async Task Handle_Should_ReturnSuccess_WithNoUpdate_When_NoValidDataToUpdate()
        {
            // Arrange
            var command = new UpdateEmailAutoTemplateCommand
            {
                TemplateId = "tpl-01"
                // không truyền gì để đổi
            };

            var existing = new EmailTemplate
            {
                TemplateId = "tpl-01",
                TemplateName = "Name 01",
                Type = EmailTemplateType.VipExpiringReminder,
                Value = 7,
                TargetGroup = UserTargetGroup.VipUsers,
                Status = EmailTemplateStatus.Draft,
                Subject = "Subject",
                Body = "Body",
                Description = "Desc",
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-1)
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.TemplateId))
                     .ReturnsAsync(existing);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(existing.TemplateId);
            result.Message.Should().Be("Không có dữ liệu hợp lệ để cập nhật!");

            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TemplateNameDuplicated()
        {
            // Arrange
            var command = new UpdateEmailAutoTemplateCommand
            {
                TemplateId = "tpl-01",
                TemplateName = "New Name"
            };

            var template = new EmailTemplate
            {
                TemplateId = "tpl-01",
                TemplateName = "Old Name",
                Type = EmailTemplateType.VipExpiringReminder,
                Value = 7,
                TargetGroup = UserTargetGroup.VipUsers,
                Status = EmailTemplateStatus.Draft,
                Subject = "S",
                Body = "B",
                Description = "D",
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-1)
            };

            // existingByName khác TemplateId và Status != Deleted => duplicated
            var duplicated = new EmailTemplate
            {
                TemplateId = "tpl-other",
                TemplateName = "New Name",
                Status = EmailTemplateStatus.Draft
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.TemplateId))
                     .ReturnsAsync(template);

            _mockRepo.Setup(x => x.GetByNameAsync("New Name"))
                     .ReturnsAsync(duplicated);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailTemplateKeyDuplicated.Code);

            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

            // dừng tại check name => không cần check config
            _mockRepo.Verify(x => x.GetByTypeValueTargetAsync(It.IsAny<EmailTemplateType>(), It.IsAny<int>(), It.IsAny<UserTargetGroup>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_ConfigDuplicated()
        {
            // Arrange
            var command = new UpdateEmailAutoTemplateCommand
            {
                TemplateId = "tpl-01",
                Value = 10 // đổi config
            };

            var template = new EmailTemplate
            {
                TemplateId = "tpl-01",
                TemplateName = "Name 01",
                Type = EmailTemplateType.VipExpiringReminder,
                Value = 7,
                TargetGroup = UserTargetGroup.VipUsers,
                Status = EmailTemplateStatus.Draft,
                Subject = "S",
                Body = "B",
                Description = "D",
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-1)
            };

            var duplicatedConfig = new EmailTemplate
            {
                TemplateId = "tpl-other",
                Status = EmailTemplateStatus.Draft
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.TemplateId))
                     .ReturnsAsync(template);

            // name không đổi => không check name
            // config đổi => check config
            _mockRepo.Setup(x => x.GetByTypeValueTargetAsync(template.Type, 10, template.TargetGroup))
                     .ReturnsAsync(duplicatedConfig);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailTemplateKeyDuplicated.Code);

            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_UpdateSubjectBodyDescription()
        {
            // Arrange
            var command = new UpdateEmailAutoTemplateCommand
            {
                TemplateId = "tpl-01",
                Subject = "New Subject",
                Body = "New Body",
                Description = "New Desc"
            };

            var template = new EmailTemplate
            {
                TemplateId = "tpl-01",
                TemplateName = "Name 01",
                Type = EmailTemplateType.VipExpiringReminder,
                Value = 7,
                TargetGroup = UserTargetGroup.VipUsers,
                Status = EmailTemplateStatus.Draft,
                Subject = "Old Subject",
                Body = "Old Body",
                Description = "Old Desc",
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-1)
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.TemplateId))
                     .ReturnsAsync(template);

            EmailTemplate? updatedEntity = null;
            _mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EmailTemplate>()))
                     .Callback<EmailTemplate>(t => updatedEntity = t)
                     .Returns(Task.CompletedTask);

            var beforeVn = DateTime.UtcNow.AddHours(7);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            var afterVn = DateTime.UtcNow.AddHours(7);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(template.TemplateId);
            result.Message.Should().Be("Cập nhật template thành công!");

            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            updatedEntity.Should().NotBeNull();
            updatedEntity!.Subject.Should().Be("New Subject");
            updatedEntity.Body.Should().Be("New Body");
            updatedEntity.Description.Should().Be("New Desc");

            updatedEntity.UpdatedAt.Should().BeOnOrAfter(beforeVn);
            updatedEntity.UpdatedAt.Should().BeOnOrBefore(afterVn);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_UpdateStatus()
        {
            // Arrange
            var command = new UpdateEmailAutoTemplateCommand
            {
                TemplateId = "tpl-01",
                Status = EmailTemplateStatus.Active
            };

            var template = new EmailTemplate
            {
                TemplateId = "tpl-01",
                TemplateName = "Name 01",
                Type = EmailTemplateType.VipExpiringReminder,
                Value = 7,
                TargetGroup = UserTargetGroup.VipUsers,
                Status = EmailTemplateStatus.Draft,
                Subject = "S",
                Body = "B",
                Description = "D",
                UpdatedAt = DateTime.UtcNow.AddHours(7).AddDays(-1)
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.TemplateId))
                     .ReturnsAsync(template);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Cập nhật template thành công!");

            template.Status.Should().Be(EmailTemplateStatus.Active);

            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task Handle_Should_ThrowException_When_SaveChangesFails()
        {
            // Arrange
            var command = new UpdateEmailAutoTemplateCommand
            {
                TemplateId = "tpl-01",
                Subject = "New Subject"
            };

            var template = new EmailTemplate
            {
                TemplateId = "tpl-01",
                TemplateName = "Name 01",
                Type = EmailTemplateType.VipExpiringReminder,
                Value = 7,
                TargetGroup = UserTargetGroup.VipUsers,
                Status = EmailTemplateStatus.Draft,
                Subject = "Old Subject",
                Body = "B",
                Description = "D"
            };

            _mockRepo.Setup(x => x.GetByIdAsync(command.TemplateId))
                     .ReturnsAsync(template);

            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("Database deadlock"));

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                          .Should().ThrowAsync<Exception>()
                          .WithMessage("Database deadlock");
        }
    }
}
