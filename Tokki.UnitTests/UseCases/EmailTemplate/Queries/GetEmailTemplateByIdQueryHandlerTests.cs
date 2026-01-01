using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailAutoTemplateById;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.EmailTemplates.Queries
{
    public class GetEmailTemplateByIdQueryHandlerTests : EmailTemplateTestBase
    {
        private readonly GetEmailTemplateByIdQueryHandler _handler;

        public GetEmailTemplateByIdQueryHandlerTests()
        {
            _handler = new GetEmailTemplateByIdQueryHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnData_When_Found()
        {
            // 1. Arrange
            string existingId = "tpl-exist";
            var query = EmailTemplateTestData.GetValidGetByIdQuery(existingId);

            var existingEntity = new EmailTemplate
            {
                TemplateId = existingId,
                TemplateKey = "FOUND_KEY",
                Subject = "Found Subject"
            };

            // Mock repo tìm thấy
            _mockRepo.Setup(x => x.GetByIdAsync(existingId))
                     .ReturnsAsync(existingEntity);

            // 2. Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data.TemplateId.Should().Be(existingId);
            result.Data.Subject.Should().Be("Found Subject");

            _mockRepo.Verify(x => x.GetByIdAsync(existingId), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_NotFound()
        {
            // 1. Arrange
            string fakeId = "tpl-not-exist";
            var query = EmailTemplateTestData.GetValidGetByIdQuery(fakeId);

            // Mock repo trả về null
            _mockRepo.Setup(x => x.GetByIdAsync(fakeId))
                     .ReturnsAsync((EmailTemplate?)null);

            // 2. Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // 3. Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Kiểm tra message lỗi cứng mà bạn đã code trong Handler:
            // "Không tìm thấy template!"
            // Lưu ý: OperationResult.Failure(string) thường gán string vào Message hoặc tạo 1 Error mặc định.
            // Tùy vào cách implement OperationResult của bạn, dòng dưới đây có thể cần điều chỉnh:
            result.Message.Should().Contain("Không tìm thấy template");
        }
    }
}