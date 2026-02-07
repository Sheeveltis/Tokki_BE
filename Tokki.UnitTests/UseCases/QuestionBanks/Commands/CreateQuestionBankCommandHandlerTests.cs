using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.QuestionBanks.Commands
{
    public class CreateQuestionBankCommandHandlerTests : QuestionBankTestBase
    {
        private readonly CreateQuestionBankCommandHandler _handler;

        public CreateQuestionBankCommandHandlerTests()
        {
            _handler = new CreateQuestionBankCommandHandler(
                _mockQuestionBankRepo.Object,
                _mockQuestionOptionRepo.Object,
                _mockQuestionTypeRepo.Object,
                _mockPassageRepo.Object,
                _mockIdGenerator.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_QuestionTypeNotFound()
        {
            // Arrange
            var command = QuestionBankTestData.GetCreateCommand(questionTypeId: "qt-01");

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuestionType?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionTypeNotFound.Code);

            _mockQuestionBankRepo.Verify(x => x.AddAsync(It.IsAny<QuestionBank>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_PassageNotFound()
        {
            // Arrange
            var command = QuestionBankTestData.GetCreateCommand(
                questionTypeId: "qt-01",
                passageId: "p-01",
                options: QuestionBankTestData.BuildCreateOptionsSingleCorrect());

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
        public async Task Handle_Should_ReturnBadRequest_When_PassageMediaTypeMismatch()
        {
            // Arrange
            var command = QuestionBankTestData.GetCreateCommand(
                questionTypeId: "qt-01",
                passageId: "p-01",
                options: QuestionBankTestData.BuildCreateOptionsSingleCorrect());

            var qt = QuestionBankTestData.BuildQuestionType("qt-01", QuestionSkill.Listening, isActive: true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            // Listening requires passage.Audio, but give Text
            var passage = QuestionBankTestData.BuildPassage("p-01", PassageMediaType.Text);
            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("p-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(passage);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Handle_Should_CreateWithoutOptions_When_SkillWriting()
        {
            // Arrange
            var command = QuestionBankTestData.GetCreateCommand(
                questionTypeId: "qt-01",
                passageId: null,
                options: new List<CreateQuestionOptionDto>
                {
                    new CreateQuestionOptionDto{ KeyOption="1", Content="A", IsCorrect=true }
                });

            var qt = QuestionBankTestData.BuildQuestionType("qt-01", QuestionSkill.Writing, isActive: true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            _mockIdGenerator.Setup(x => x.GenerateCustom(10)).Returns("qb-99");

            _mockQuestionBankRepo.Setup(x => x.AddAsync(It.IsAny<QuestionBank>())).Returns(Task.CompletedTask);
            _mockQuestionBankRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("qb-99");

            _mockQuestionOptionRepo.Verify(x => x.AddRangeAsync(It.IsAny<List<QuestionOption>>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_CreateWithOptions_When_NotWriting()
        {
            // Arrange
            var command = QuestionBankTestData.GetCreateCommand(
                questionTypeId: "qt-01",
                passageId: null,
                options: QuestionBankTestData.BuildCreateOptionsSingleCorrect());

            var qt = QuestionBankTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, isActive: true);

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            // CHỈ DÙNG SetupSequence (bỏ setup dư)
            _mockIdGenerator.SetupSequence(x => x.GenerateCustom(10))
                .Returns("qb-99")   // QuestionBankId
                .Returns("opt-01")  // option 1
                .Returns("opt-02"); // option 2

            IEnumerable<QuestionOption>? capturedOptions = null;

            _mockQuestionBankRepo
                .Setup(x => x.AddAsync(It.IsAny<QuestionBank>()))
                .Returns(Task.CompletedTask);

            // Setup theo IEnumerable để chắc chắn match chữ ký interface
            _mockQuestionOptionRepo
                .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionOption>>()))
                .Callback<IEnumerable<QuestionOption>>(opts => capturedOptions = opts)
                .Returns(Task.CompletedTask);

            _mockQuestionBankRepo
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // nếu SaveChangesAsync trả bool trong project bạn

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("qb-99");

            capturedOptions.Should().NotBeNull();

            var optsList = capturedOptions!.ToList();
            optsList.Should().HaveCount(2);
            optsList.Should().OnlyContain(o => o.QuestionBankId == "qb-99");

            _mockQuestionOptionRepo.Verify(
                x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionOption>>()),
                Times.Once);

            _mockQuestionBankRepo.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnServerError_When_Exception()
        {
            // Arrange
            var command = QuestionBankTestData.GetCreateCommand(questionTypeId: "qt-01");

            var qt = QuestionBankTestData.BuildQuestionType("qt-01", QuestionSkill.Reading, isActive: true);
            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qt);

            _mockIdGenerator.Setup(x => x.GenerateCustom(10)).Returns("qb-99");

            _mockQuestionBankRepo
                .Setup(x => x.AddAsync(It.IsAny<QuestionBank>()))
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
