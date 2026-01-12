using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.QuestionBanks.Commands
{
    public class UpdateQuestionBankCommandHandlerTests : QuestionBankTestBase
    {
        private readonly UpdateQuestionBankCommandHandler _handler;

        public UpdateQuestionBankCommandHandlerTests()
        {
            _handler = new UpdateQuestionBankCommandHandler(
                _mockQuestionBankRepo.Object,
                _mockQuestionTypeRepo.Object,
                _mockPassageRepo.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_QuestionBankNotExists()
        {
            // Arrange
            var command = QuestionBankTestData.GetUpdateCommand(id: "qb-01");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuestionBank?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionBankNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_StatusNotDraft()
        {
            // Arrange
            var command = QuestionBankTestData.GetUpdateCommand(id: "qb-01");

            var qb = QuestionBankTestData.BuildQuestionBank(id: "qb-01", status: QuestionBankStatus.Active);

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors.Should().Contain(e => e.Code == AppErrors.Forbidden.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_CannotDetermineQuestionTypeId()
        {
            // Arrange
            var command = QuestionBankTestData.GetUpdateCommand(id: "qb-01", questionTypeId: "   ");

            var qb = QuestionBankTestData.BuildQuestionBank(id: "qb-01", status: QuestionBankStatus.Draft, questionTypeId: "   ");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Không xác định được QuestionTypeId");
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_NewQuestionTypeNotFound()
        {
            // Arrange
            var command = QuestionBankTestData.GetUpdateCommand(id: "qb-01", questionTypeId: "qt-02");

            var qb = QuestionBankTestData.BuildQuestionBank(id: "qb-01", status: QuestionBankStatus.Draft, questionTypeId: "qt-01");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-02", It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuestionType?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionTypeNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_NewQuestionTypeInactive()
        {
            // Arrange
            var command = QuestionBankTestData.GetUpdateCommand(id: "qb-01", questionTypeId: "qt-02");

            var qb = QuestionBankTestData.BuildQuestionBank(id: "qb-01", status: QuestionBankStatus.Draft, questionTypeId: "qt-01");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionBankTestData.BuildQuestionType("qt-02", QuestionSkill.Reading, isActive: false);

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-02", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("Loại câu hỏi đang bị vô hiệu hóa.");
        }

        [Fact]
        public async Task Handle_Should_BlockChangeToWriting_When_HasOptions()
        {
            // Arrange
            var command = QuestionBankTestData.GetUpdateCommand(id: "qb-01", questionTypeId: "qt-writing");

            var qb = QuestionBankTestData.BuildQuestionBank(
                id: "qb-01",
                status: QuestionBankStatus.Draft,
                questionTypeId: "qt-old",
                options: new List<QuestionOption> { QuestionBankTestData.BuildOption("opt-01", "qb-01", "1", "A", true) });

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var newQt = QuestionBankTestData.BuildQuestionType("qt-writing", QuestionSkill.Writing, isActive: true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-writing", It.IsAny<CancellationToken>()))
                .ReturnsAsync(newQt);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Không thể chuyển sang Writing");
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_PassageNotFound()
        {
            // Arrange
            var command = QuestionBankTestData.GetUpdateCommand(id: "qb-01", questionTypeId: "qt-01", passageId: "p-01");

            var qb = QuestionBankTestData.BuildQuestionBank(id: "qb-01", status: QuestionBankStatus.Draft, questionTypeId: "qt-01");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionBankTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, isActive: true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("p-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Passage?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.PassageNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_Update_And_ReturnSuccess_When_Valid()
        {
            // Arrange
            var command = QuestionBankTestData.GetUpdateCommand(
                id: "qb-01",
                questionTypeId: "qt-01",
                passageId: "p-01",
                content: "updated",
                mediaUrl: "m",
                explanation: "e");

            var qb = QuestionBankTestData.BuildQuestionBank(id: "qb-01", status: QuestionBankStatus.Draft, questionTypeId: "qt-01");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionBankTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, isActive: true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            var passage = QuestionBankTestData.BuildPassage("p-01", PassageMediaType.Text);
            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("p-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(passage);

            _mockQuestionBankRepo.Setup(x => x.UpdateAsync(It.IsAny<QuestionBank>())).Returns(Task.CompletedTask);
            _mockQuestionBankRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("qb-01");

            qb.PassageId.Should().Be("p-01");
            qb.QuestionTypeId.Should().Be("qt-01");
            qb.Content.Should().Be("updated");
            qb.MediaUrl.Should().Be("m");
            qb.Explanation.Should().Be("e");

            _mockQuestionBankRepo.Verify(x => x.UpdateAsync(It.IsAny<QuestionBank>()), Times.Once);
            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
