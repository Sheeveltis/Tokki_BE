using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.EmailTemplates.Commands
{
    public class CreateEmailAutoTemplateCommandHandlerTests : EmailTemplateTestBase
    {
        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TemplateNameDuplicated_And_NotDeleted()
        {
            // Arrange
            var command = EmailTemplateTestData.GetValidCreateEmailAutoTemplateCommand();
            var existing = EmailTemplateTestData.GetExistingTemplateByName(command.TemplateName, EmailTemplateStatus.Draft);

            _mockRepo.Setup(x => x.GetByNameAsync(command.TemplateName))
                     .ReturnsAsync(existing);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailTemplateKeyDuplicated.Code);

            _mockRepo.Verify(x => x.AddAsync(It.IsAny<EmailTemplate>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

            // Vì fail ngay ở check name, không cần check logic
            _mockRepo.Verify(x => x.GetByTypeValueTargetAsync(It.IsAny<EmailTemplateType>(), It.IsAny<int>(), It.IsAny<UserTargetGroup>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Continue_When_TemplateNameDuplicated_But_Deleted()
        {
            // Arrange
            var command = EmailTemplateTestData.GetValidCreateEmailAutoTemplateCommand();

            // Name trùng nhưng status Deleted => được phép tạo mới
            var deleted = EmailTemplateTestData.GetExistingTemplateByName(command.TemplateName, EmailTemplateStatus.Deleted);

            _mockRepo.Setup(x => x.GetByNameAsync(command.TemplateName))
                     .ReturnsAsync(deleted);

            // Logic không trùng
            _mockRepo.Setup(x => x.GetByTypeValueTargetAsync(command.Type, command.Value, command.TargetGroup))
                     .ReturnsAsync((EmailTemplate?)null);

            _mockIdGen.Setup(x => x.Generate(15)).Returns("tpl-new-01");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("tpl-new-01");
            result.Message.Should().Be("Tạo template thành công!");

            _mockRepo.Verify(x => x.AddAsync(It.Is<EmailTemplate>(t =>
                t.TemplateId == "tpl-new-01" &&
                t.TemplateName == command.TemplateName &&
                t.Type == command.Type &&
                t.Value == command.Value &&
                t.TargetGroup == command.TargetGroup &&
                t.Status == EmailTemplateStatus.Draft &&
                t.Subject == command.Subject &&
                t.Body == command.Body &&
                t.Description == command.Description &&
                t.CreateAt != default &&
                t.UpdatedAt != default
            )), Times.Once);

            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TypeValueTargetDuplicated_And_NotDeleted()
        {
            // Arrange
            var command = EmailTemplateTestData.GetValidCreateEmailAutoTemplateCommand();

            // Name không trùng
            _mockRepo.Setup(x => x.GetByNameAsync(command.TemplateName))
                     .ReturnsAsync((EmailTemplate?)null);

            // Logic trùng và not deleted => fail
            var existingLogic = EmailTemplateTestData.GetExistingTemplateByLogic(command.Type, command.Value, command.TargetGroup, EmailTemplateStatus.Draft);

            _mockRepo.Setup(x => x.GetByTypeValueTargetAsync(command.Type, command.Value, command.TargetGroup))
                     .ReturnsAsync(existingLogic);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailTemplateKeyDuplicated.Code);

            _mockRepo.Verify(x => x.AddAsync(It.IsAny<EmailTemplate>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_InputIsValid()
        {
            // Arrange
            var command = EmailTemplateTestData.GetValidCreateEmailAutoTemplateCommand();

            _mockRepo.Setup(x => x.GetByNameAsync(command.TemplateName))
                     .ReturnsAsync((EmailTemplate?)null);

            _mockRepo.Setup(x => x.GetByTypeValueTargetAsync(command.Type, command.Value, command.TargetGroup))
                     .ReturnsAsync((EmailTemplate?)null);

            _mockIdGen.Setup(x => x.Generate(15)).Returns("tpl-new-02");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("tpl-new-02");
            result.Message.Should().Be("Tạo template thành công!");

            _mockRepo.Verify(x => x.AddAsync(It.Is<EmailTemplate>(t =>
                t.TemplateId == "tpl-new-02" &&
                t.TemplateName == command.TemplateName &&
                t.Type == command.Type &&
                t.Value == command.Value &&
                t.TargetGroup == command.TargetGroup &&
                t.Status == EmailTemplateStatus.Draft &&
                t.Subject == command.Subject &&
                t.Body == command.Body &&
                t.Description == command.Description
            )), Times.Once);

            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowException_When_SaveChangesFails()
        {
            // Arrange
            var command = EmailTemplateTestData.GetValidCreateEmailAutoTemplateCommand();

            _mockRepo.Setup(x => x.GetByNameAsync(command.TemplateName))
                     .ReturnsAsync((EmailTemplate?)null);

            _mockRepo.Setup(x => x.GetByTypeValueTargetAsync(command.Type, command.Value, command.TargetGroup))
                     .ReturnsAsync((EmailTemplate?)null);

            _mockIdGen.Setup(x => x.Generate(15)).Returns("tpl-new-03");

            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("DB Connection Timeout"));

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                          .Should().ThrowAsync<Exception>()
                          .WithMessage("DB Connection Timeout");
        }
    }
}
