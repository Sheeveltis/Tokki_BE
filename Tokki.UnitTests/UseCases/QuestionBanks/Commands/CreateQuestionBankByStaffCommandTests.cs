using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBankByStaff;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Xunit;

namespace Tokki.UnitTests.Features.QuestionBanks.Commands
{
    public class CreateQuestionBankByStaffCommandHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _mockQuestionBankRepo = new();
        private readonly Mock<IQuestionOptionRepository> _mockQuestionOptionRepo = new();
        private readonly Mock<IQuestionTypeRepository> _mockQuestionTypeRepo = new();
        private readonly Mock<IPassageRepository> _mockPassageRepo = new();
        private readonly Mock<IIdGeneratorService> _mockIdGenerator = new();

        private readonly CreateQuestionBankByStaffCommandHandler _handler;

        public CreateQuestionBankByStaffCommandHandlerTests()
        {
            _handler = new CreateQuestionBankByStaffCommandHandler(
                _mockQuestionBankRepo.Object,
                _mockQuestionOptionRepo.Object,
                _mockQuestionTypeRepo.Object,
                _mockPassageRepo.Object,
                _mockIdGenerator.Object
            );
        }

        private static CreateQuestionBankByStaffCommand BuildCommand(
            string? questionTypeId = "qt-01",
            QuestionSkill? skillHint = null,
            string content = "content",
            string? mediaUrl = "https://audio",
            string? passageId = null,
            string? explanation = "explain",
            string? createBy = "staff-01",
            List<CreateQuestionOptionDto>? options = null)
        {
            return new CreateQuestionBankByStaffCommand
            {
                QuestionTypeId = questionTypeId,
                PassageId = passageId,
                Content = content,
                MediaUrl = mediaUrl,
                Explanation = explanation,
                CreateBy = createBy,
                Options = options ?? new List<CreateQuestionOptionDto>
                {
                    new CreateQuestionOptionDto { KeyOption = "1", Content = "A", IsCorrect = true },
                    new CreateQuestionOptionDto { KeyOption = "2", Content = "B", IsCorrect = false }
                }
            };
        }

        private static QuestionType BuildQuestionType(string id, QuestionSkill skill, bool isActive = true)
        {
            return new QuestionType
            {
                QuestionTypeId = id,
                Skill = skill,
                IsActive = isActive,
                Name = "QT"
            };
        }

        private static Passage BuildPassage(string id, PassageMediaType mediaType)
        {
            return new Passage
            {
                PassageId = id,
                MediaType = mediaType,
                Title = "P"
            };
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_QuestionTypeId_Invalid()
        {
            // Arrange
            var cmd = BuildCommand(questionTypeId: "   ");

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ValidationFailed.Code);
            result.Message.Should().Contain("QuestionTypeId is not valid");
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_QuestionType_NotFound()
        {
            // Arrange
            var cmd = BuildCommand(questionTypeId: "qt-404");

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-404", It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuestionType?)null);

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionTypeNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_QuestionType_Inactive()
        {
            // Arrange
            var cmd = BuildCommand(questionTypeId: "qt-01");

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildQuestionType("qt-01", QuestionSkill.Reading, isActive: false));

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("Question types are disabled.");
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_Listening_MissingMediaUrl()
        {
            // Arrange
            var cmd = BuildCommand(questionTypeId: "qt-listen", mediaUrl: "   ");

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-listen", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildQuestionType("qt-listen", QuestionSkill.Listening, isActive: true));

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Listening requires MediaUrl");
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_Reading_MissingContent()
        {
            // Arrange
            var cmd = BuildCommand(questionTypeId: "qt-read", content: "   ");

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-read", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildQuestionType("qt-read", QuestionSkill.Reading, isActive: true));

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Reading is required to have Content");
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_Passage_NotFound()
        {
            // Arrange
            var cmd = BuildCommand(questionTypeId: "qt-read", passageId: "p-01", content: "ok");

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-read", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildQuestionType("qt-read", QuestionSkill.Reading, isActive: true));

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("p-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Passage?)null);

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.PassageNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_PassageMediaType_Mismatch()
        {
            // Arrange
            var cmd = BuildCommand(questionTypeId: "qt-listen", passageId: "p-01", mediaUrl: "m");

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-listen", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildQuestionType("qt-listen", QuestionSkill.Listening, isActive: true));

            // Listening yêu cầu passage audio, nhưng trả text => mismatch
            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("p-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildPassage("p-01", PassageMediaType.Text));

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("Failure.");
        }
        [Fact]
        public async Task Handle_Should_CreateDraftReading_And_AddOptions_When_Valid()
        {
            // Arrange
            var cmd = BuildCommand(
                questionTypeId: "qt-read",
                content: " updated content ",
                mediaUrl: "  https://m  ",
                passageId: "  p-01  ",
                createBy: "staff-01",
                options: new List<CreateQuestionOptionDto>
                {
            new CreateQuestionOptionDto { KeyOption = "1", Content = "A", IsCorrect = true },
            new CreateQuestionOptionDto { KeyOption = "2", Content = "B", IsCorrect = false }
                });

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-read", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildQuestionType("qt-read", QuestionSkill.Reading, isActive: true));

            _mockPassageRepo
                .Setup(x => x.GetByIdAsync("p-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildPassage("p-01", PassageMediaType.Text));

            _mockIdGenerator
                .SetupSequence(x => x.GenerateCustom(10))
                .Returns("qb-01")
                .Returns("opt-01")
                .Returns("opt-02");

            QuestionBank? capturedQb = null;
            _mockQuestionBankRepo
                .Setup(x => x.AddAsync(It.IsAny<QuestionBank>()))
                .Callback<QuestionBank>(qb => capturedQb = qb)
                .Returns(Task.CompletedTask);

            List<QuestionOption>? capturedOptions = null;

            // Nếu interface là AddRangeAsync(IEnumerable<QuestionOption>)
            _mockQuestionOptionRepo
                .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionOption>>()))
                .Callback<IEnumerable<QuestionOption>>(opts => capturedOptions = opts.ToList())
                .Returns(Task.CompletedTask);

            // Nếu interface của bạn là AddRangeAsync(List<QuestionOption>) thì dùng block này thay cho block trên:
            // _mockQuestionOptionRepo
            //     .Setup(x => x.AddRangeAsync(It.IsAny<List<QuestionOption>>()))
            //     .Callback<List<QuestionOption>>(opts => capturedOptions = opts)
            //     .Returns(Task.CompletedTask);

            // SaveChangesAsync: chọn đúng theo signature interface của bạn
            _mockQuestionBankRepo
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // nếu Task<bool>
                                     // .Returns(Task.CompletedTask); // nếu Task

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert - result
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("qb-01");
            result.Message.Should().Contain("Draft");

            // Assert - captured question bank
            capturedQb.Should().NotBeNull();
            capturedQb!.QuestionBankId.Should().Be("qb-01");
            capturedQb.QuestionTypeId.Should().Be("qt-read");
            capturedQb.PassageId.Should().Be("p-01");
            capturedQb.MediaUrl.Should().Be("https://m");
            capturedQb.Content.Should().Be(" updated content ");
            capturedQb.Explanation.Should().Be(cmd.Explanation);
            capturedQb.Status.Should().Be(QuestionBankStatus.Draft);
            capturedQb.CreateBy.Should().Be("staff-01");
            capturedQb.CreatedAt.Should().NotBe(default);

            // Assert - options added
            capturedOptions.Should().NotBeNull();
            capturedOptions!.Should().HaveCount(2);

            capturedOptions.Select(o => o.QuestionBankId).Distinct().Single().Should().Be("qb-01");
            capturedOptions.Select(o => o.KeyOption).Should().BeEquivalentTo(new[] { "1", "2" });

            // Verify calls
            _mockQuestionBankRepo.Verify(x => x.AddAsync(It.IsAny<QuestionBank>()), Times.Once);
            _mockQuestionOptionRepo.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionOption>>()), Times.Once);
            // Nếu bạn dùng overload List<QuestionOption> thì verify tương ứng:
            // _mockQuestionOptionRepo.Verify(x => x.AddRangeAsync(It.IsAny<List<QuestionOption>>()), Times.Once);

            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Generator should be called: 1 QB + 2 option
            _mockIdGenerator.Verify(x => x.GenerateCustom(10), Times.Exactly(3));
        }

        [Fact]
        public async Task Handle_Should_CreateDraftWriting_And_NotAddOptions_When_Valid()
        {
            // Arrange
            var cmd = BuildCommand(
                questionTypeId: "qt-write",
                content: "prompt",
                mediaUrl: null,
                passageId: null,
                options: new List<CreateQuestionOptionDto>() // writing: thường rỗng
            );

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-write", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildQuestionType("qt-write", QuestionSkill.Writing, isActive: true));

            _mockIdGenerator
                .Setup(x => x.GenerateCustom(10))
                .Returns("qb-10");

            _mockQuestionBankRepo
                .Setup(x => x.AddAsync(It.IsAny<QuestionBank>()))
                .Returns(Task.CompletedTask);

            _mockQuestionBankRepo
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("qb-10");

            // writing => không add options
            _mockQuestionOptionRepo.Verify(x => x.AddRangeAsync(It.IsAny<List<QuestionOption>>()), Times.Never);
        }
    }

    public class CreateQuestionBankByStaffCommandValidatorTests
    {
        private readonly Mock<IQuestionTypeRepository> _mockQuestionTypeRepo = new();

        private CreateQuestionBankByStaffCommandValidator CreateValidator()
            => new CreateQuestionBankByStaffCommandValidator(_mockQuestionTypeRepo.Object);

        private static CreateQuestionBankByStaffCommand BuildCmd(
            string? questionTypeId,
            string content = "content",
            string? mediaUrl = "m",
            List<CreateQuestionOptionDto>? options = null)
        {
            return new CreateQuestionBankByStaffCommand
            {
                QuestionTypeId = questionTypeId,
                Content = content,
                MediaUrl = mediaUrl,
                Options = options ?? new List<CreateQuestionOptionDto>
                {
                    new CreateQuestionOptionDto{ KeyOption="1", Content="A", IsCorrect=true },
                    new CreateQuestionOptionDto{ KeyOption="2", Content="B", IsCorrect=false },
                }
            };
        }

        private static QuestionType BuildQuestionType(string id, QuestionSkill skill, bool isActive = true)
        {
            return new QuestionType
            {
                QuestionTypeId = id,
                Skill = skill,
                IsActive = isActive,
                Name = "QT"
            };
        }

        [Fact]
        public async Task Validator_Should_Fail_When_QuestionType_NotFound()
        {
            // Arrange
            var validator = CreateValidator();
            var cmd = BuildCmd("qt-404");

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-404", It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuestionType?)null);

            // Act
            var result = await validator.ValidateAsync(cmd, CancellationToken.None);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == AppErrors.QuestionTypeNotFound.Description);
        }

        [Fact]
        public async Task Validator_Should_Fail_When_QuestionType_Inactive()
        {
            // Arrange
            var validator = CreateValidator();
            var cmd = BuildCmd("qt-01");

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildQuestionType("qt-01", QuestionSkill.Reading, isActive: false));

            // Act
            var result = await validator.ValidateAsync(cmd, CancellationToken.None);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "Question types are disabled.");
        }

        [Fact]
        public async Task Validator_Should_Fail_When_Listening_MissingMediaUrl()
        {
            // Arrange
            var validator = CreateValidator();
            var cmd = BuildCmd("qt-listen", mediaUrl: "   ");

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-listen", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildQuestionType("qt-listen", QuestionSkill.Listening, isActive: true));

            // Act
            var result = await validator.ValidateAsync(cmd, CancellationToken.None);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Listening requires MediaUrl"));
        }

        [Fact]
        public async Task Validator_Should_Fail_When_Reading_MissingContent()
        {
            // Arrange
            var validator = CreateValidator();
            var cmd = BuildCmd("qt-read", content: "   ");

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-read", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildQuestionType("qt-read", QuestionSkill.Reading, isActive: true));

            // Act
            var result = await validator.ValidateAsync(cmd, CancellationToken.None);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Reading is required to have Content"));
        }

        [Fact]
        public async Task Validator_Should_Fail_When_Writing_HasOptions()
        {
            // Arrange
            var validator = CreateValidator();
            var cmd = BuildCmd(
                "qt-write",
                content: "prompt",
                options: new List<CreateQuestionOptionDto>
                {
                    new CreateQuestionOptionDto{ KeyOption="1", Content="A", IsCorrect=true }
                });

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-write", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildQuestionType("qt-write", QuestionSkill.Writing, isActive: true));

            // Act
            var result = await validator.ValidateAsync(cmd, CancellationToken.None);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == AppErrors.WritingNoOptions.Description);
        }

        [Fact]
        public async Task Validator_Should_Fail_When_OptionsCount_Invalid_ForReading()
        {
            // Arrange
            var validator = CreateValidator();
            var cmd = BuildCmd(
                "qt-read",
                options: new List<CreateQuestionOptionDto> // chỉ 1 option => invalid (2-4)
                {
                    new CreateQuestionOptionDto{ KeyOption="1", Content="A", IsCorrect=true }
                });

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-read", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildQuestionType("qt-read", QuestionSkill.Reading, isActive: true));

            // Act
            var result = await validator.ValidateAsync(cmd, CancellationToken.None);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == AppErrors.QuestionBankInvalidOptions.Description);
        }

        [Fact]
        public async Task Validator_Should_Pass_When_Reading_Valid()
        {
            // Arrange
            var validator = CreateValidator();
            var cmd = BuildCmd(
                "qt-read",
                content: "ok",
                options: new List<CreateQuestionOptionDto>
                {
                    new CreateQuestionOptionDto{ KeyOption="1", Content="A", IsCorrect=true },
                    new CreateQuestionOptionDto{ KeyOption="2", Content="B", IsCorrect=false }
                });

            _mockQuestionTypeRepo
                .Setup(x => x.GetByIdAsync("qt-read", It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildQuestionType("qt-read", QuestionSkill.Reading, isActive: true));

            // Act
            var result = await validator.ValidateAsync(cmd, CancellationToken.None);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }
}
