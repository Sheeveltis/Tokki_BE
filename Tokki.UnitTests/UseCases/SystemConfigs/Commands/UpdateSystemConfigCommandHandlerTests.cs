using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.SystemConfigs.Commands.Update;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.SystemConfigs.Commands
{
    public class UpdateSystemConfigCommandHandlerTests : SystemConfigTestBase
    {
        private readonly UpdateSystemConfigCommandHandler _handler;

        public UpdateSystemConfigCommandHandlerTests()
        {
            _handler = new UpdateSystemConfigCommandHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_ConfigNotFound()
        {
            // Arrange
            var command = SystemConfigTestData.BuildUpdateCommand(key: "CFG_NOT_FOUND");

            _mockRepo
                .Setup(x => x.GetByKeyAsync("CFG_NOT_FOUND"))
                .ReturnsAsync((SystemConfig?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.ConfigNotFound.Code);

            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_UpdateAndReturn200_When_Valid()
        {
            // Arrange
            var command = SystemConfigTestData.BuildUpdateCommand(
                key: "CFG_TEST",
                value: "999",
                description: "updated",
                isActive: false);

            var entity = SystemConfigTestData.BuildEntity(
                key: "CFG_TEST",
                value: "1",
                description: "old",
                isActive: true);

            _mockRepo
                .Setup(x => x.GetByKeyAsync("CFG_TEST"))
                .ReturnsAsync(entity);

            _mockRepo
    .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("CFG_TEST");
            result.Message.Should().Be("Cập nhật thành công");

            entity.Value.Should().Be("999");
            entity.Description.Should().Be("updated");
            entity.IsActive.Should().BeFalse();
            entity.UpdatedAt.Should().NotBeNull();

            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
