using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Update;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.QuestionBanks.QuestionOptions.Commands
{
    public class UpdateQuestionOptionCommandHandlerTests : QuestionOptionTestBase
    {
        private readonly UpdateQuestionOptionCommandHandler _handler;

        public UpdateQuestionOptionCommandHandlerTests()
        {
            _handler = new UpdateQuestionOptionCommandHandler(
                _mockQuestionBankRepo.Object,
                _mockQuestionOptionRepo.Object,
                _mockQuestionTypeRepo.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_QuestionBankNotFound()
        {
            var command = QuestionOptionTestData.BuildUpdateCommand("qb-01", "opt-01", content: "X");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuestionBank?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionBankNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_QuestionBankNotDraft()
        {
            var command = QuestionOptionTestData.BuildUpdateCommand("qb-01", "opt-01", content: "X");

            var qb = QuestionOptionTestData.BuildQuestionBank("qb-01", QuestionBankStatus.Active, "qt-01");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors.Should().Contain(e => e.Code == AppErrors.Forbidden.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_QuestionTypeIdEmptyInQuestionBank()
        {
            var command = QuestionOptionTestData.BuildUpdateCommand("qb-01", "opt-01", content: "X");

            var qb = QuestionOptionTestData.BuildQuestionBank("qb-01", QuestionBankStatus.Draft, "   ");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ValidationFailed.Code);
            result.Message.Should().Be("QuestionTypeId of the question is empty.");
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_QuestionTypeNotFound()
        {
            var command = QuestionOptionTestData.BuildUpdateCommand("qb-01", "opt-01", content: "X");

            var qb = QuestionOptionTestData.BuildQuestionBank("qb-01", QuestionBankStatus.Draft, "qt-01");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuestionType?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionTypeNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_SkillIsWriting()
        {
            var command = QuestionOptionTestData.BuildUpdateCommand("qb-01", "opt-01", content: "X");

            var qb = QuestionOptionTestData.BuildQuestionBank("qb-01", QuestionBankStatus.Draft, "qt-01");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionOptionTestData.BuildQuestionType("qt-01", QuestionSkill.Writing, true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("Writing questions cannot have multiple choice answers.");
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_OptionNotFound()
        {
            var command = QuestionOptionTestData.BuildUpdateCommand("qb-01", "opt-99", content: "X");

            var qb = QuestionOptionTestData.BuildQuestionBank(
                "qb-01",
                QuestionBankStatus.Draft,
                "qt-01",
                options: new List<QuestionOption>
                {
                    QuestionOptionTestData.BuildOption("opt-01", "qb-01", "1")
                });

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionOptionTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionOptionNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_KeyOptionDuplicated()
        {
            var command = QuestionOptionTestData.BuildUpdateCommand(
                questionBankId: "qb-01",
                optionId: "opt-01",
                keyOption: "2");

            var option1 = QuestionOptionTestData.BuildOption("opt-01", "qb-01", keyOption: "1");
            var option2 = QuestionOptionTestData.BuildOption("opt-02", "qb-01", keyOption: "2");

            var qb = QuestionOptionTestData.BuildQuestionBank(
                "qb-01",
                QuestionBankStatus.Draft,
                "qt-01",
                options: new List<QuestionOption> { option1, option2 });

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionOptionTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("KeyOption '2' already exists");
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_OptionHasNoContentAndNoImage()
        {
            // option hiện tại invalid: Content=null & ImageUrl=null
            var command = QuestionOptionTestData.BuildUpdateCommand(
                questionBankId: "qb-01",
                optionId: "opt-01",
                isCorrect: true); // chỉ đổi correct, không đổi content/image

            var opt = QuestionOptionTestData.BuildOption("opt-01", "qb-01", keyOption: "1", content: null, imageUrl: null, isCorrect: false);

            var qb = QuestionOptionTestData.BuildQuestionBank(
                "qb-01",
                QuestionBankStatus.Draft,
                "qt-01",
                options: new List<QuestionOption> { opt });

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionOptionTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("The answer must have text or image content.");

            _mockQuestionOptionRepo.Verify(x => x.UpdateAsync(It.IsAny<QuestionOption>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_UnsetOnlyCorrectAnswer()
        {
            var command = QuestionOptionTestData.BuildUpdateCommand(
                questionBankId: "qb-01",
                optionId: "opt-01",
                isCorrect: false);

            var opt1 = QuestionOptionTestData.BuildOption("opt-01", "qb-01", "1", content: "A", isCorrect: true);
            var opt2 = QuestionOptionTestData.BuildOption("opt-02", "qb-01", "2", content: "B", isCorrect: false);

            var qb = QuestionOptionTestData.BuildQuestionBank(
                "qb-01",
                QuestionBankStatus.Draft,
                "qt-01",
                options: new List<QuestionOption> { opt1, opt2 });

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionOptionTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("The question must have at least one correct answer. The only correct answer cannot be unchecked.");
        }

        [Fact]
        public async Task Handle_Should_SetCorrectTrue_And_UnsetOtherCorrectOptions()
        {
            var command = QuestionOptionTestData.BuildUpdateCommand(
                questionBankId: "qb-01",
                optionId: "opt-01",
                isCorrect: true);

            var target = QuestionOptionTestData.BuildOption("opt-01", "qb-01", "1", content: "A", isCorrect: false);
            var otherCorrect = QuestionOptionTestData.BuildOption("opt-02", "qb-01", "2", content: "B", isCorrect: true);

            var qb = QuestionOptionTestData.BuildQuestionBank(
                "qb-01",
                QuestionBankStatus.Draft,
                "qt-01",
                options: new List<QuestionOption> { target, otherCorrect });

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionOptionTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            _mockQuestionOptionRepo.Setup(x => x.UpdateAsync(It.IsAny<QuestionOption>())).Returns(Task.CompletedTask);
            _mockQuestionOptionRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("opt-01");
            result.Message.Should().Be("Updated answer successfully.");

            otherCorrect.IsCorrect.Should().BeFalse();
            target.IsCorrect.Should().BeTrue();

            // 1 lần update otherCorrect + 1 lần update target
            _mockQuestionOptionRepo.Verify(x => x.UpdateAsync(It.IsAny<QuestionOption>()), Times.Exactly(2));
            _mockQuestionOptionRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_UpdateFields_And_ReturnSuccess_When_Valid()
        {
            var command = QuestionOptionTestData.BuildUpdateCommand(
                questionBankId: "qb-01",
                optionId: "opt-01",
                keyOption: " 3 ",
                content: "New content",
                imageUrl: " https://img ",
                isCorrect: null);

            var target = QuestionOptionTestData.BuildOption("opt-01", "qb-01", "1", content: "Old", imageUrl: null, isCorrect: false);

            var qb = QuestionOptionTestData.BuildQuestionBank(
                "qb-01",
                QuestionBankStatus.Draft,
                "qt-01",
                options: new List<QuestionOption> { target });

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionOptionTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            _mockQuestionOptionRepo.Setup(x => x.UpdateAsync(It.IsAny<QuestionOption>())).Returns(Task.CompletedTask);
            _mockQuestionOptionRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("opt-01");

            target.KeyOption.Should().Be("3");
            target.Content.Should().Be("New content");
            target.ImageUrl.Should().Be("https://img");

            _mockQuestionOptionRepo.Verify(x => x.UpdateAsync(It.Is<QuestionOption>(o => o.OptionId == "opt-01")), Times.Once);
            _mockQuestionOptionRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
