using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Delete;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.QuestionBanks.QuestionOptions.Commands
{
    public class DeleteQuestionOptionCommandHandlerTests : QuestionOptionTestBase
    {
        private readonly DeleteQuestionOptionCommandHandler _handler;

        public DeleteQuestionOptionCommandHandlerTests()
        {
            _handler = new DeleteQuestionOptionCommandHandler(
                _mockQuestionBankRepo.Object,
                _mockQuestionOptionRepo.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_QuestionBankNotFound()
        {
            var command = QuestionOptionTestData.BuildDeleteCommand("qb-01", "opt-01");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuestionBank?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionBankNotFound.Code);

            _mockQuestionOptionRepo.Verify(x => x.DeleteAsync(It.IsAny<QuestionOption>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_QuestionBankNotDraft()
        {
            var command = QuestionOptionTestData.BuildDeleteCommand("qb-01", "opt-01");

            var qb = QuestionOptionTestData.BuildQuestionBank("qb-01", QuestionBankStatus.Active);

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionBankNeedToHaveDraftStatus.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_OptionNotFound()
        {
            var command = QuestionOptionTestData.BuildDeleteCommand("qb-01", "opt-99");

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

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionOptionNotFound.Code);

            _mockQuestionOptionRepo.Verify(x => x.DeleteAsync(It.IsAny<QuestionOption>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_DeleteOption_And_ReturnSuccess_When_Valid()
        {
            var command = QuestionOptionTestData.BuildDeleteCommand("qb-01", "opt-01");

            var option = QuestionOptionTestData.BuildOption("opt-01", "qb-01", "1");
            var qb = QuestionOptionTestData.BuildQuestionBank(
                "qb-01",
                QuestionBankStatus.Draft,
                "qt-01",
                options: new List<QuestionOption> { option });

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdWithDetailsAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            _mockQuestionOptionRepo.Setup(x => x.DeleteAsync(It.IsAny<QuestionOption>())).Returns(Task.CompletedTask);
            _mockQuestionOptionRepo
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeTrue();
            result.Message.Should().Be("Xóa đáp án thành công.");

            _mockQuestionOptionRepo.Verify(x => x.DeleteAsync(It.Is<QuestionOption>(o => o.OptionId == "opt-01")), Times.Once);
            _mockQuestionOptionRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
