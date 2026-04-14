using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.Commands.DeleteQuestionBank;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.QuestionBanks.Commands
{
    public class DeleteQuestionBankCommandHandlerTests : QuestionBankTestBase
    {
        private readonly DeleteQuestionBankCommandHandler _handler;

        public DeleteQuestionBankCommandHandlerTests()
        {
            _handler = new DeleteQuestionBankCommandHandler(
                _mockQuestionBankRepo.Object,
                _mockQuestionOptionRepo.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_NotExists()
        {
            // Arrange
            var command = QuestionBankTestData.GetDeleteCommand("qb-01");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tokki.Domain.Entities.QuestionBank?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionBankNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnConflict_When_AlreadyDeleted()
        {
            // Arrange
            var command = QuestionBankTestData.GetDeleteCommand("qb-01");
            var qb = QuestionBankTestData.BuildQuestionBank(id: "qb-01", status: QuestionBankStatus.Deleted);

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionBankHasDeleted.Code);

            _mockQuestionOptionRepo.Verify(x => x.DeleteByQuestionBankIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_DeleteOptions_And_SoftDeleteQuestionBank()
        {
            // Arrange
            var command = QuestionBankTestData.GetDeleteCommand("qb-01");
            var qb = QuestionBankTestData.BuildQuestionBank(id: "qb-01", status: QuestionBankStatus.Draft);

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            _mockQuestionOptionRepo
                .Setup(x => x.DeleteByQuestionBankIdAsync("qb-01", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockQuestionBankRepo
                .Setup(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.QuestionBank>()))
                .Returns(Task.CompletedTask);

            _mockQuestionBankRepo
         .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
         .ReturnsAsync(true);


            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Successfully deleted QuestionBank"); // ✅ đúng message handler

            qb.Status.Should().Be(QuestionBankStatus.Deleted);

            _mockQuestionOptionRepo.Verify(
                x => x.DeleteByQuestionBankIdAsync("qb-01", It.IsAny<CancellationToken>()),
                Times.Once);

            _mockQuestionBankRepo.Verify(
                x => x.UpdateAsync(It.Is<Tokki.Domain.Entities.QuestionBank>(q => q.QuestionBankId == "qb-01" && q.Status == QuestionBankStatus.Deleted)),
                Times.Once);

            _mockQuestionBankRepo.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnServerError_When_Exception()
        {
            // Arrange
            var command = QuestionBankTestData.GetDeleteCommand("qb-01");
            var qb = QuestionBankTestData.BuildQuestionBank(id: "qb-01", status: QuestionBankStatus.Draft);

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            _mockQuestionOptionRepo
                .Setup(x => x.DeleteByQuestionBankIdAsync("qb-01", It.IsAny<CancellationToken>()))
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
