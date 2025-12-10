using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.EmailTemplates.Commands
{
    public class UpdateEmailTemplateCommandHandlerTests : EmailTemplateTestBase
    {
        // Khai báo Handler riêng cho class test này
        private readonly UpdateEmailTemplateCommandHandler _handler;

        public UpdateEmailTemplateCommandHandlerTests()
        {
            // Khởi tạo UpdateHandler sử dụng _mockRepo từ Base class
            _handler = new UpdateEmailTemplateCommandHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TemplateNotFound()
        {
            // 1. Arrange
            var command = EmailTemplateTestData.GetUpdateCommandWithNonExistentId();

            // Giả lập Repository: Tìm ID không thấy -> trả về null
            _mockRepo.Setup(x => x.GetByIdAsync(command.TemplateId))
                     .ReturnsAsync((EmailTemplate?)null);

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeFalse();
            // Kiểm tra lỗi trả về có đúng Code "EmailTemplate.NotFound" (tương ứng AppErrors.EmailTemplateNotFound)
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailTemplateNotFound.Code);

            // Đảm bảo không gọi Update hay SaveChanges
            _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EmailTemplate>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_UpdateIsValid()
        {
            // 1. Arrange
            string existingId = "template-exist-01";
            var command = EmailTemplateTestData.GetValidUpdateEmailTemplateCommand(existingId);

            // Tạo entity cũ đang nằm trong DB
            var existingEntity = new EmailTemplate
            {
                TemplateId = existingId,
                Subject = "Subject Cũ",
                Body = "Body Cũ",
                Description = "Desc Cũ",
                UpdatedAt = DateTime.UtcNow.AddDays(-1) // Thời gian cũ
            };

            // Giả lập Repository: Tìm thấy entity
            _mockRepo.Setup(x => x.GetByIdAsync(existingId))
                     .ReturnsAsync(existingEntity);

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Cập nhật template thành công!");

            // Kiểm tra xem Entity đã được cập nhật giá trị mới từ Command chưa
            existingEntity.Subject.Should().Be(command.Subject);
            existingEntity.Body.Should().Be(command.Body);
            existingEntity.Description.Should().Be(command.Description);

            // Kiểm tra hàm UpdateAsync và SaveChangesAsync đã được gọi
            _mockRepo.Verify(x => x.UpdateAsync(existingEntity), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowException_When_DatabaseSaveFails()
        {
            // 1. Arrange
            string existingId = "template-exist-01";
            var command = EmailTemplateTestData.GetValidUpdateEmailTemplateCommand(existingId);
            var existingEntity = new EmailTemplate { TemplateId = existingId };

            _mockRepo.Setup(x => x.GetByIdAsync(existingId)).ReturnsAsync(existingEntity);

            // Giả lập lỗi khi gọi SaveChanges
            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("Database deadlock"));

            // 2. Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                          .Should().ThrowAsync<Exception>()
                          .WithMessage("Database deadlock");
        }
    }
}