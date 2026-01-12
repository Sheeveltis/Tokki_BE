using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Create;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.QuestionBanks.QuestionOptions.Commands
{
    public class CreateQuestionOptionCommandHandlerTests : QuestionOptionTestBase
    {
        private readonly CreateQuestionOptionCommandHandler _handler;

        public CreateQuestionOptionCommandHandlerTests()
        {
            _handler = new CreateQuestionOptionCommandHandler(
                _mockQuestionBankRepo.Object,
                _mockQuestionOptionRepo.Object,
                _mockQuestionTypeRepo.Object,
                _mockIdGenerator.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_QuestionBankNotFound()
        {
            var command = QuestionOptionTestData.BuildCreateCommand(questionBankId: "qb-01");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuestionBank?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionBankNotFound.Code);

            _mockQuestionOptionRepo.Verify(x => x.AddAsync(It.IsAny<QuestionOption>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_QuestionBankNotDraft()
        {
            var command = QuestionOptionTestData.BuildCreateCommand(questionBankId: "qb-01");

            var qb = QuestionOptionTestData.BuildQuestionBank("qb-01", QuestionBankStatus.Active);

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
            var command = QuestionOptionTestData.BuildCreateCommand(questionBankId: "qb-01");

            var qb = QuestionOptionTestData.BuildQuestionBank("qb-01", QuestionBankStatus.Draft, questionTypeId: "   ");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ValidationFailed.Code);
            result.Message.Should().Be("QuestionTypeId của câu hỏi đang rỗng.");
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_QuestionTypeNotFound()
        {
            var command = QuestionOptionTestData.BuildCreateCommand(questionBankId: "qb-01");

            var qb = QuestionOptionTestData.BuildQuestionBank("qb-01", QuestionBankStatus.Draft, questionTypeId: "qt-01");

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
            var command = QuestionOptionTestData.BuildCreateCommand(questionBankId: "qb-01");

            var qb = QuestionOptionTestData.BuildQuestionBank("qb-01", QuestionBankStatus.Draft, questionTypeId: "qt-01");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionOptionTestData.BuildQuestionType("qt-01", QuestionSkill.Writing, isActive: true);

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("Câu hỏi Writing không được có đáp án trắc nghiệm.");
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_ExceedMax4Options()
        {
            var command = QuestionOptionTestData.BuildCreateCommand(questionBankId: "qb-01", keyOption: "4");

            var qb = QuestionOptionTestData.BuildQuestionBank(
                questionBankId: "qb-01",
                status: QuestionBankStatus.Draft,
                questionTypeId: "qt-01",
                options: QuestionOptionTestData.BuildOptions(4));

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionOptionTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, isActive: true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("Không thể thêm quá 4 đáp án.");
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_KeyOptionDuplicated()
        {
            var command = QuestionOptionTestData.BuildCreateCommand(questionBankId: "qb-01", keyOption: "1");

            var qb = QuestionOptionTestData.BuildQuestionBank(
                questionBankId: "qb-01",
                status: QuestionBankStatus.Draft,
                questionTypeId: "qt-01",
                options: new List<QuestionOption>
                {
                    QuestionOptionTestData.BuildOption(optionId:"opt-01", questionBankId:"qb-01", keyOption:"1")
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
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("KeyOption '1' đã tồn tại");
        }

        [Fact]
        public async Task Handle_Should_UnsetOtherCorrectOptions_When_RequestIsCorrectTrue()
        {
            var command = QuestionOptionTestData.BuildCreateCommand(
                questionBankId: "qb-01",
                keyOption: "2",
                content: "B",
                isCorrect: true);

            var existingCorrect = QuestionOptionTestData.BuildOption(optionId: "opt-01", questionBankId: "qb-01", keyOption: "1", isCorrect: true);
            var qb = QuestionOptionTestData.BuildQuestionBank(
                questionBankId: "qb-01",
                status: QuestionBankStatus.Draft,
                questionTypeId: "qt-01",
                options: new List<QuestionOption> { existingCorrect });

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionOptionTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            _mockIdGenerator.Setup(x => x.GenerateCustom(10)).Returns("opt-99");

            _mockQuestionOptionRepo.Setup(x => x.UpdateAsync(It.IsAny<QuestionOption>())).Returns(Task.CompletedTask);
            _mockQuestionOptionRepo.Setup(x => x.AddAsync(It.IsAny<QuestionOption>())).Returns(Task.CompletedTask);
            _mockQuestionOptionRepo
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("opt-99");

            existingCorrect.IsCorrect.Should().BeFalse();

            _mockQuestionOptionRepo.Verify(x => x.UpdateAsync(It.Is<QuestionOption>(o => o.OptionId == "opt-01")), Times.Once);
            _mockQuestionOptionRepo.Verify(x => x.AddAsync(It.IsAny<QuestionOption>()), Times.Once);
            _mockQuestionOptionRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_CreateOption_And_Return201_When_Valid()
        {
            var command = QuestionOptionTestData.BuildCreateCommand(
                questionBankId: "qb-01",
                keyOption: " 3 ",
                content: " C ",
                imageUrl: "  ",
                isCorrect: false);

            var qb = QuestionOptionTestData.BuildQuestionBank("qb-01", QuestionBankStatus.Draft, "qt-01", options: new List<QuestionOption>());

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var qt = QuestionOptionTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            _mockIdGenerator.Setup(x => x.GenerateCustom(10)).Returns("opt-99");

            QuestionOption? captured = null;

            _mockQuestionOptionRepo
                .Setup(x => x.AddAsync(It.IsAny<QuestionOption>()))
                .Callback<QuestionOption>(o => captured = o)
                .Returns(Task.CompletedTask);

            _mockQuestionOptionRepo
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("opt-99");
            result.Message.Should().Be("Thêm đáp án thành công.");

            captured.Should().NotBeNull();
            captured!.OptionId.Should().Be("opt-99");
            captured.QuestionBankId.Should().Be("qb-01");
            captured.KeyOption.Should().Be("3");
            captured.Content.Should().Be(" C "); // handler không trim content
            captured.ImageUrl.Should().BeNull();  // "  " => null
            captured.IsCorrect.Should().BeFalse();
        }
    }
}
