using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailAutoTemplateById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Xunit;

namespace Tokki.UnitTests.Features.EmailTemplates.Queries
{
    public class GetEmailAutoTemplateByIdQueryHandlerTests : EmailTemplateTestBase
    {
        private readonly GetEmailAutoTemplateByIdQueryHandler _handler;

        public GetEmailAutoTemplateByIdQueryHandlerTests()
        {
            _handler = new GetEmailAutoTemplateByIdQueryHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_Found_And_NotDeleted()
        {
            // Arrange
            string existingId = "tpl-exist";
            var query = new GetEmailAutoTemplateByIdQuery { TemplateId = existingId };

            var existingEntity = new EmailTemplate
            {
                TemplateId = existingId,
                TemplateName = "FOUND_NAME",
                Subject = "Found Subject",
                Status = EmailTemplateStatus.Active
            };

            _mockRepo.Setup(x => x.GetByIdAsync(existingId))
                     .ReturnsAsync(existingEntity);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Get the template successfully");

            result.Data.Should().NotBeNull();
            result.Data!.TemplateId.Should().Be(existingId);
            result.Data.Subject.Should().Be("Found Subject");

            _mockRepo.Verify(x => x.GetByIdAsync(existingId), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TemplateId_IsEmpty()
        {
            // Arrange
            var query = new GetEmailAutoTemplateByIdQuery { TemplateId = "" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().NotBeNull();
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailTemplateNotFound.Code);

            // Không gọi repo vì id không hợp lệ
            _mockRepo.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_NotFound()
        {
            // Arrange
            string fakeId = "tpl-not-exist";
            var query = new GetEmailAutoTemplateByIdQuery { TemplateId = fakeId };

            _mockRepo.Setup(x => x.GetByIdAsync(fakeId))
                     .ReturnsAsync((EmailTemplate?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().NotBeNull();
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailTemplateNotFound.Code);

            _mockRepo.Verify(x => x.GetByIdAsync(fakeId), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_Template_IsDeleted()
        {
            // Arrange
            string existingId = "tpl-deleted";
            var query = new GetEmailAutoTemplateByIdQuery { TemplateId = existingId };

            var deletedEntity = new EmailTemplate
            {
                TemplateId = existingId,
                TemplateName = "DELETED",
                Subject = "Deleted Subject",
                Status = EmailTemplateStatus.Deleted
            };

            _mockRepo.Setup(x => x.GetByIdAsync(existingId))
                     .ReturnsAsync(deletedEntity);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().NotBeNull();
            result.Errors.Should().Contain(e => e.Code == AppErrors.EmailTemplateNotFound.Code);

            _mockRepo.Verify(x => x.GetByIdAsync(existingId), Times.Once);
        }
    }
}
