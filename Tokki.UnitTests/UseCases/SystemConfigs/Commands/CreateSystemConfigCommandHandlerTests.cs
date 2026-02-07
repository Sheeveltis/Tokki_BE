using FluentAssertions;
using FluentValidation;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.SystemConfigs.Commands.Create;
using Tokki.Domain.Entities;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.SystemConfigs.Commands
{
    public class CreateSystemConfigCommandHandlerTests : SystemConfigTestBase
    {
        private readonly Mock<IValidator<CreateSystemConfigCommand>> _mockValidator;
        private readonly CreateSystemConfigCommandHandler _handler;

        public CreateSystemConfigCommandHandlerTests()
        {
            _mockValidator = new Mock<IValidator<CreateSystemConfigCommand>>(MockBehavior.Loose);

            _handler = new CreateSystemConfigCommandHandler(
                _mockRepo.Object,
                _mockValidator.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_KeyDuplicated()
        {
            // Arrange
            var command = SystemConfigTestData.BuildCreateCommand(key: "CFG_MAX_ITEMS");

            _mockRepo
                .Setup(x => x.GetByKeyAsync("CFG_MAX_ITEMS"))
                .ReturnsAsync(SystemConfigTestData.BuildEntity(key: "CFG_MAX_ITEMS"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == Tokki.Application.Common.Models.AppErrors.ConfigKeyDuplicated.Code);

            _mockRepo.Verify(x => x.AddAsync(It.IsAny<SystemConfig>()), Times.Never);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_CreateAndReturn201_When_Valid()
        {
            // Arrange
            var command = SystemConfigTestData.BuildCreateCommand(
                key: "CFG_TEST",
                value: "abc",
                description: "d",
                dataType: "string");

            _mockRepo
                .Setup(x => x.GetByKeyAsync("CFG_TEST"))
                .ReturnsAsync((SystemConfig?)null);

            SystemConfig? captured = null;

            _mockRepo
                .Setup(x => x.AddAsync(It.IsAny<SystemConfig>()))
                .Callback<SystemConfig>(e => captured = e)
                .Returns(Task.CompletedTask);

            _mockRepo
      .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("CFG_TEST");
            result.Message.Should().Be("Tạo cấu hình thành công");

            captured.Should().NotBeNull();
            captured!.Key.Should().Be("CFG_TEST");
            captured.Value.Should().Be("abc");
            captured.Description.Should().Be("d");
            captured.DataType.Should().Be("string");
            captured.IsActive.Should().BeTrue();

            _mockRepo.Verify(x => x.AddAsync(It.IsAny<SystemConfig>()), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
