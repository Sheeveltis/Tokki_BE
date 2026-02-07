using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.Commands.ActivateQuestionBanks;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.QuestionBanks.Commands
{
    public class ActivateQuestionBanksCommandHandlerTests : QuestionBankTestBase
    {
        private readonly ActivateQuestionBanksCommandHandler _handler;

        public ActivateQuestionBanksCommandHandlerTests()
        {
            _handler = new ActivateQuestionBanksCommandHandler(
                _mockQuestionBankRepo.Object,
                _mockActivateLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_IdsInvalid()
        {
            // Arrange
            var command = QuestionBankTestData.GetActivateCommand("   ", null!, "");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.BadRequest.Code);

            _mockQuestionBankRepo.Verify(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_SomeIdsMissing()
        {
            // Arrange
            var command = QuestionBankTestData.GetActivateCommand("qb-01", "qb-02");

            var found = new List<Tokki.Domain.Entities.QuestionBank>
            {
                QuestionBankTestData.BuildQuestionBank(id: "qb-01", status: QuestionBankStatus.Draft)
            };

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdsAsync(It.Is<List<string>>(ids => ids.Count == 2), It.IsAny<CancellationToken>()))
                .ReturnsAsync(found);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionBankNotFound.Code);
            result.Message.Should().Contain("qb-02");

            _mockQuestionBankRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<List<Tokki.Domain.Entities.QuestionBank>>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_AnyNotDraft()
        {
            // Arrange
            var command = QuestionBankTestData.GetActivateCommand("qb-01", "qb-02");

            var items = new List<Tokki.Domain.Entities.QuestionBank>
            {
                QuestionBankTestData.BuildQuestionBank(id: "qb-01", status: QuestionBankStatus.Draft),
                QuestionBankTestData.BuildQuestionBank(id: "qb-02", status: QuestionBankStatus.Active)
            };

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(items);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors.Should().Contain(e => e.Code == AppErrors.Forbidden.Code);
            result.Message.Should().Contain("qb-02");

            _mockQuestionBankRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<List<Tokki.Domain.Entities.QuestionBank>>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ActivateAll_And_ReturnSuccess()
        {
            // Arrange
            var command = QuestionBankTestData.GetActivateCommand(" qb-01 ", "qb-02", "qb-02");

            var items = new List<Tokki.Domain.Entities.QuestionBank>
            {
                QuestionBankTestData.BuildQuestionBank(id: "qb-01", status: QuestionBankStatus.Draft),
                QuestionBankTestData.BuildQuestionBank(id: "qb-02", status: QuestionBankStatus.Draft),
            };

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(items);

            _mockQuestionBankRepo
                .Setup(x => x.UpdateRangeAsync(It.IsAny<List<Tokki.Domain.Entities.QuestionBank>>()))
                .Returns(Task.CompletedTask);

            _mockQuestionBankRepo
             .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);


            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(2);

            items[0].Status.Should().Be(QuestionBankStatus.Active);
            items[1].Status.Should().Be(QuestionBankStatus.Active);

            _mockQuestionBankRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<List<Tokki.Domain.Entities.QuestionBank>>()), Times.Once);
            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnServerError_When_Exception()
        {
            // Arrange
            var command = QuestionBankTestData.GetActivateCommand("qb-01");

            var items = new List<Tokki.Domain.Entities.QuestionBank>
            {
                QuestionBankTestData.BuildQuestionBank(id: "qb-01", status: QuestionBankStatus.Draft)
            };

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(items);

            _mockQuestionBankRepo
                .Setup(x => x.UpdateRangeAsync(It.IsAny<List<Tokki.Domain.Entities.QuestionBank>>()))
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
