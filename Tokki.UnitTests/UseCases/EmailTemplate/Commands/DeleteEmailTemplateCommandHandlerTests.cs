using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailTemplate;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.EmailTemplates.Commands
{
    public class DeleteEmailTemplateCommandHandlerTests : EmailTemplateTestBase
    {
        private readonly DeleteEmailTemplateCommandHandler _handler;

        public DeleteEmailTemplateCommandHandlerTests()
        {
            // Khởi tạo Handler với Mock Repository từ Base
            _handler = new DeleteEmailTemplateCommandHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TemplateNotFound()
        {
            // 1. Arrange
            string fakeId = "template-fake-id";
            var command = EmailTemplateTestData.GetValidDeleteCommand(fakeId);

            // Giả lập Repository: Tìm ID không thấy -> trả về null
            _mockRepo.Setup(x => x.GetByIdAsync(fakeId))
                     .ReturnsAsync((EmailTemplate?)null);

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeFalse();
            // Kiểm tra mã lỗi trả về
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailTemplateNotFound.Code);

            // QUAN TRỌNG: Đảm bảo hàm DeleteAsync KHÔNG được gọi
            _mockRepo.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_TemplateExists()
        {
            // 1. Arrange
            string existingId = "template-real-id";
            var command = EmailTemplateTestData.GetValidDeleteCommand(existingId);
            var existingEntity = new EmailTemplate { TemplateId = existingId };

            // Giả lập Repository: Tìm thấy entity
            _mockRepo.Setup(x => x.GetByIdAsync(existingId))
                     .ReturnsAsync(existingEntity);

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Xóa template thành công!");

            // Kiểm tra hàm DeleteAsync đã được gọi đúng ID
            _mockRepo.Verify(x => x.DeleteAsync(existingId), Times.Once);

            // Kiểm tra hàm SaveChangesAsync đã được gọi
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowException_When_DatabaseErrorOccurs()
        {
            // 1. Arrange
            string existingId = "template-error-id";
            var command = EmailTemplateTestData.GetValidDeleteCommand(existingId);
            var existingEntity = new EmailTemplate { TemplateId = existingId };

            _mockRepo.Setup(x => x.GetByIdAsync(existingId)).ReturnsAsync(existingEntity);

            // Giả lập lỗi tại hàm Delete hoặc SaveChanges
            _mockRepo.Setup(x => x.DeleteAsync(existingId))
                     .ThrowsAsync(new Exception("Foreign Key Constraint Error"));

            // 2. Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                          .Should().ThrowAsync<Exception>()
                          .WithMessage("Foreign Key Constraint Error");
        }
    }
}