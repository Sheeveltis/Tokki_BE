using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models; // Để dùng AppErrors
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.EmailTemplates.Commands
{
    public class CreateEmailTemplateCommandHandlerTests : EmailTemplateTestBase
    {
        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TemplateKeyDuplicated()
        {
            // 1. Arrange (Chuẩn bị dữ liệu)
            var command = EmailTemplateTestData.GetValidCreateEmailTemplateCommand();
            var existingTemplate = EmailTemplateTestData.GetExistingEmailTemplate();

            // Giả lập Repository: Tìm thấy key này đã tồn tại trong DB
            _mockRepo.Setup(x => x.GetByKeyAsync(command.TemplateKey))
                     .ReturnsAsync(existingTemplate);

            // 2. Act (Thực hiện hành động)
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert (Kiểm tra kết quả)
            result.IsSuccess.Should().BeFalse();

            // Kiểm tra lỗi trả về có đúng là EmailTemplateKeyDuplicated không
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailTemplateKeyDuplicated.Code);

            // Đảm bảo không gọi hàm AddAsync
            _mockRepo.Verify(x => x.AddAsync(It.IsAny<EmailTemplate>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_InputIsValid()
        {
            // 1. Arrange
            var command = EmailTemplateTestData.GetValidCreateEmailTemplateCommand();

            // Giả lập Repository: Chưa có key này (trả về null)
            _mockRepo.Setup(x => x.GetByKeyAsync(command.TemplateKey))
                     .ReturnsAsync((EmailTemplate?)null);

            // Giả lập ID Generator sinh ra ID
            string generatedId = "template-new-01";
            _mockIdGen.Setup(x => x.Generate(15)).Returns(generatedId);

            // 2. Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be(generatedId); // Kiểm tra ID trả về

            // Kiểm tra hàm AddAsync được gọi đúng 1 lần với dữ liệu chính xác
            _mockRepo.Verify(x => x.AddAsync(It.Is<EmailTemplate>(t =>
                t.TemplateId == generatedId &&
                t.TemplateKey == command.TemplateKey &&
                t.Subject == command.Subject &&
                t.Body == command.Body
            )), Times.Once);

            // Kiểm tra hàm SaveChangesAsync được gọi
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowException_When_DatabaseFails()
        {
            // Lưu ý: Trong code Handler của bạn KHÔNG có try-catch block.
            // Nên nếu DB lỗi, nó sẽ ném Exception ra ngoài thay vì trả về Result Error.
            // Test này kiểm tra việc ném Exception đó.

            // 1. Arrange
            var command = EmailTemplateTestData.GetValidCreateEmailTemplateCommand();

            _mockRepo.Setup(x => x.GetByKeyAsync(command.TemplateKey))
                     .ReturnsAsync((EmailTemplate?)null);

            // Giả lập lỗi khi lưu xuống DB
            _mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("DB Connection Timeout"));

            // 2. Act & Assert
            // Mong đợi hành động này sẽ ném ra Exception
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                          .Should().ThrowAsync<Exception>()
                          .WithMessage("DB Connection Timeout");
        }
    }
}