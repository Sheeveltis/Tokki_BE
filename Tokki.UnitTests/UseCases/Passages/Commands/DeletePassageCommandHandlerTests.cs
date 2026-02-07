using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Passages.Commands.DeletePassage;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Passages.Commands
{
    public class DeletePassageCommandHandlerTests : PassageTestBase
    {
        private readonly DeletePassageCommandHandler _handler;

        public DeletePassageCommandHandlerTests()
        {
            _handler = new DeletePassageCommandHandler(
                _mockPassageRepo.Object,
                _mockQuestionBankRepo.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_PassageNotExists()
        {
            // Arrange
            var command = PassageTestData.GetDeleteCommand("  pass-01  ");

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tokki.Domain.Entities.Passage?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.PassageNotFound.Code);

            _mockQuestionBankRepo.Verify(x => x.AnyUsingPassageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnConflict_When_PassageInUse()
        {
            // Arrange
            var command = PassageTestData.GetDeleteCommand("pass-01");
            var passage = PassageTestData.BuildPassage(passageId: "pass-01", status: PassageStatus.Active);

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(passage);

            _mockQuestionBankRepo
                .Setup(x => x.AnyUsingPassageAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            result.Errors.Should().Contain(e => e.Code == AppErrors.PassageInUse.Code);

            _mockPassageRepo.Verify(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Passage>()), Times.Never);
            _mockPassageRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_AlreadyHidden()
        {
            // Arrange
            var command = PassageTestData.GetDeleteCommand("pass-01");
            var passage = PassageTestData.BuildPassage(passageId: "pass-01", status: PassageStatus.Hidden);

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(passage);

            _mockQuestionBankRepo
                .Setup(x => x.AnyUsingPassageAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Đoạn văn đã bị ẩn trước đó.");

            _mockPassageRepo.Verify(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Passage>()), Times.Never);
            _mockPassageRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_HidePassage_And_ReturnSuccess_When_Valid()
        {
            // Arrange
            var command = PassageTestData.GetDeleteCommand("pass-01");
            var passage = PassageTestData.BuildPassage(passageId: "pass-01", status: PassageStatus.Active);

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(passage);

            _mockQuestionBankRepo
                .Setup(x => x.AnyUsingPassageAsync("pass-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockPassageRepo
                .Setup(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Passage>()))
                .Returns(Task.CompletedTask);

            _mockPassageRepo
        .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);


            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Xóa đoạn văn thành công.");

            passage.Status.Should().Be(PassageStatus.Hidden);

            _mockPassageRepo.Verify(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Passage>()), Times.Once);
            _mockPassageRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnServerError_When_ExceptionThrown()
        {
            // Arrange
            var command = PassageTestData.GetDeleteCommand("pass-01");

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("pass-01", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("db"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ServerError.Code);
        }
    }
}
