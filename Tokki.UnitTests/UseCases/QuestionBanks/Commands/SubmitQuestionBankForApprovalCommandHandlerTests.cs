using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.Commands.SubmitQuestionBankForApproval;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Xunit;

namespace Tokki.UnitTests.Features.QuestionBanks.Commands
{
    public class SubmitQuestionBankForApprovalCommandHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _mockQuestionBankRepo;
        private readonly SubmitQuestionBankForApprovalCommandHandler _handler;

        public SubmitQuestionBankForApprovalCommandHandlerTests()
        {
            _mockQuestionBankRepo = new Mock<IQuestionBankRepository>();
            _handler = new SubmitQuestionBankForApprovalCommandHandler(_mockQuestionBankRepo.Object);
        }

        private static SubmitQuestionBankForApprovalCommand BuildCommand(params string[] ids)
        {
            return new SubmitQuestionBankForApprovalCommand
            {
                QuestionBankIds = ids?.ToList() ?? new List<string>()
            };
        }

        private static QuestionBank BuildQb(
            string id,
            QuestionBankStatus status,
            string? approvedBy = "old-approver",
            DateTime? approvedDate = null)
        {
            return new QuestionBank
            {
                QuestionBankId = id,
                Status = status,
                ApprovedBy = approvedBy,
                ApprovedDate = approvedDate ?? DateTime.UtcNow
            };
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_IdsEmptyOrInvalid()
        {
            // Arrange: toàn whitespace/empty => ids sau normalize sẽ rỗng
            var command = BuildCommand("   ", "", " \t ");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ValidationFailed.Code);
            result.Message.Should().Contain("The QuestionBankIds list is empty or invalid");
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_QuestionBankNotFound()
        {
            // Arrange
            var command = BuildCommand("qb-404");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdAsync("qb-404", It.IsAny<CancellationToken>()))
                .ReturnsAsync((QuestionBank?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionBankNotFound.Code);
            result.Message.Should().Contain("qb-404");

            _mockQuestionBankRepo.Verify(x => x.UpdateAsync(It.IsAny<QuestionBank>()), Times.Never);
            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(QuestionBankStatus.Active)]
        [InlineData(QuestionBankStatus.PendingApproval)]
        public async Task Handle_Should_ReturnBadRequest_When_StatusNotDraftOrRejected(QuestionBankStatus invalidStatus)
        {
            // Arrange
            var command = BuildCommand("qb-01");

            var qb = BuildQb("qb-01", invalidStatus);

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ValidationFailed.Code);
            result.Message.Should().Contain("qb-01");
            result.Message.Should().Contain("Draft or Rejected");

            _mockQuestionBankRepo.Verify(x => x.UpdateAsync(It.IsAny<QuestionBank>()), Times.Never);
            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_SubmitSuccessfully_When_AllValid_And_Should_Trim_Distinct()
        {
            // Arrange: có khoảng trắng + trùng id
            var command = BuildCommand(" qb-01 ", "qb-02", "qb-01");

            var qb1 = BuildQb("qb-01", QuestionBankStatus.Draft, approvedBy: "old", approvedDate: DateTime.UtcNow);
            var qb2 = BuildQb("qb-02", QuestionBankStatus.Rejected, approvedBy: "old2", approvedDate: DateTime.UtcNow);

            // handler sẽ gọi GetByIdAsync với id đã Trim()
            _mockQuestionBankRepo
                .Setup(x => x.GetByIdAsync("qb-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb1);

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdAsync("qb-02", It.IsAny<CancellationToken>()))
                .ReturnsAsync(qb2);

            _mockQuestionBankRepo
                .Setup(x => x.UpdateAsync(It.IsAny<QuestionBank>()))
                .Returns(Task.CompletedTask);

            _mockQuestionBankRepo
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Submitted successfully for approval.");

            // Distinct theo id sau Trim => chỉ còn qb-01, qb-02
            result.Data.Should().NotBeNull();
            result.Data.Should().Equal(new List<string> { "qb-01", "qb-02" });

            // QB được cập nhật status + reset approval fields
            qb1.Status.Should().Be(QuestionBankStatus.PendingApproval);
            qb1.ApprovedBy.Should().BeNull();
            qb1.ApprovedDate.Should().BeNull();

            qb2.Status.Should().Be(QuestionBankStatus.PendingApproval);
            qb2.ApprovedBy.Should().BeNull();
            qb2.ApprovedDate.Should().BeNull();

            _mockQuestionBankRepo.Verify(
                x => x.GetByIdAsync("qb-01", It.IsAny<CancellationToken>()),
                Times.Once);

            _mockQuestionBankRepo.Verify(
                x => x.GetByIdAsync("qb-02", It.IsAny<CancellationToken>()),
                Times.Once);

            // Update mỗi id unique đúng 1 lần
            _mockQuestionBankRepo.Verify(x => x.UpdateAsync(It.Is<QuestionBank>(q => q.QuestionBankId == "qb-01")), Times.Once);
            _mockQuestionBankRepo.Verify(x => x.UpdateAsync(It.Is<QuestionBank>(q => q.QuestionBankId == "qb-02")), Times.Once);

            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class SubmitQuestionBankForApprovalCommandValidatorTests
    {
        private readonly SubmitQuestionBankForApprovalCommandValidator _validator = new();

        [Fact]
        public void Validator_Should_Fail_When_QuestionBankIds_Empty()
        {
            var cmd = new SubmitQuestionBankForApprovalCommand
            {
                QuestionBankIds = new List<string>()
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "The QuestionBankIds list is invalid.");
        }
        [Fact]
        public void Validator_Should_Fail_When_QuestionBankIds_AllWhitespace()
        {
            var cmd = new SubmitQuestionBankForApprovalCommand
            {
                QuestionBankIds = new List<string> { "   ", "", "\t" }
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "The QuestionBankIds list is invalid.");
        }

        [Fact]
        public void Validator_Should_Fail_When_AllIdsWhitespace()
        {
            var cmd = new SubmitQuestionBankForApprovalCommand
            {
                QuestionBankIds = new List<string> { "   ", "", "\t" }
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("The QuestionBankIds list is invalid"));
        }

        [Fact]
        public void Validator_Should_Fail_When_Duplicate_AfterTrim()
        {
            var cmd = new SubmitQuestionBankForApprovalCommand
            {
                QuestionBankIds = new List<string> { " qb-01 ", "qb-01" }
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("List of duplicate QuestionBankIds"));
        }

        [Fact]
        public void Validator_Should_Pass_When_Unique_AfterTrim()
        {
            var cmd = new SubmitQuestionBankForApprovalCommand
            {
                QuestionBankIds = new List<string> { " qb-01 ", "qb-02" }
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeTrue();
        }
    }
}
