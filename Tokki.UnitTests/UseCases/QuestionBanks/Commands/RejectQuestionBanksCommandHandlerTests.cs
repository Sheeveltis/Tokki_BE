using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.RejectQuestionBank;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Xunit;

namespace Tokki.UnitTests.Features.QuestionBanks.Commands
{
    public class RejectQuestionBanksCommandHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _mockQuestionBankRepo = new();
        private readonly Mock<IAccountRepository> _mockAccountRepo = new();
        private readonly Mock<IEmailService> _mockEmailService = new();
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor = new();
        private readonly Mock<ILogger<RejectQuestionBanksCommandHandler>> _mockLogger = new();

        private RejectQuestionBanksCommandHandler CreateHandlerWithUser(string? userId)
        {
            var httpContext = new DefaultHttpContext();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                httpContext.User = new ClaimsPrincipal(
                    new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "TestAuth"));
            }
            else
            {
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            }

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            return new RejectQuestionBanksCommandHandler(
                _mockQuestionBankRepo.Object,
                _mockAccountRepo.Object,
                _mockEmailService.Object,
                _mockHttpContextAccessor.Object,
                _mockLogger.Object
            );
        }

        private static RejectQuestionBanksCommand BuildCommand(string reason, params string[] ids)
        {
            return new RejectQuestionBanksCommand
            {
                RejectReason = reason,
                QuestionBankIds = ids?.ToList() ?? new List<string>()
            };
        }

        private static QuestionBank BuildQb(string id, QuestionBankStatus status, string? createBy = null)
        {
            return new QuestionBank
            {
                QuestionBankId = id,
                Status = status,
                CreateBy = createBy,
                ApprovedBy = "old-approver",
                ApprovedDate = DateTime.UtcNow.AddDays(-1),

                // để build mail body không null
                QuestionTypeId = "qt-01",
                PassageId = "p-01",
                Content = "c",
                MediaUrl = "m",
                Explanation = "e",
                QuestionOptions = new List<QuestionOption>
                {
                    new QuestionOption { KeyOption = "1", Content = "A", IsCorrect = true },
                    new QuestionOption { KeyOption = "2", Content = "B", IsCorrect = false },
                }
            };
        }

        [Fact]
        public async Task Handle_Should_ReturnUnauthorized_When_NoUserId()
        {
            // Arrange
            var handler = CreateHandlerWithUser(userId: null);
            var cmd = BuildCommand(reason: "reason", ids: "qb-01");

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            result.Errors.Should().Contain(e => e.Code == AppErrors.UserUnauthorized.Code);

            _mockQuestionBankRepo.Verify(
                x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

      
        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_RejectReason_Missing()
        {
            // Arrange
            var handler = CreateHandlerWithUser("staff-01");
            var cmd = BuildCommand(reason: "   ", ids: "qb-01");

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == "REJECT_REASON_REQUIRED");
            result.Message.Should().Contain("Lack of reason for refusal");

            _mockQuestionBankRepo.Verify(
                x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

          [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_AnyQbDeleted()
        {
            // Arrange
            var handler = CreateHandlerWithUser("staff-01");
            var cmd = BuildCommand(reason: "reason", ids: "qb-01");

            var qb01 = BuildQb("qb-01", QuestionBankStatus.Deleted, createBy: "u1");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuestionBank> { qb01 });

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ValidationFailed.Code);
            result.Message.Should().Contain("has been deleted");

            _mockQuestionBankRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<List<QuestionBank>>()), Times.Never);
            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_StatusNotPendingApproval_AndNotAlreadyRejected()
        {
            // Arrange
            var handler = CreateHandlerWithUser("staff-01");
            var cmd = BuildCommand(reason: "reason", ids: "qb-01");

            var qb01 = BuildQb("qb-01", QuestionBankStatus.Draft, createBy: "u1");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuestionBank> { qb01 });

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ValidationFailed.Code);
            result.Message.Should().Contain("not in PendingApproval state");

            _mockQuestionBankRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<List<QuestionBank>>()), Times.Never);
            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_BeIdempotent_When_AlreadyRejected_And_NotSendEmail()
        {
            // Arrange
            var handler = CreateHandlerWithUser("staff-01");
            var cmd = BuildCommand(reason: "reason", ids: "qb-01");

            var qb01 = BuildQb("qb-01", QuestionBankStatus.Rejected, createBy: "u1");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuestionBank> { qb01 });

            _mockQuestionBankRepo
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Equal(new List<string> { "qb-01" });

            _mockQuestionBankRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<List<QuestionBank>>()), Times.Never);
            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // không mail lại
            _mockAccountRepo.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

      
    }

    public class RejectQuestionBanksCommandValidatorTests
    {
        private readonly RejectQuestionBanksCommandValidator _validator = new();

        [Fact]
        public void Validator_Should_Fail_When_Ids_Empty()
        {
            var cmd = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string>(),
                RejectReason = "reason"
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "The question code list cannot be empty.");
        }

        [Fact]
        public void Validator_Should_Fail_When_Ids_AllWhitespace()
        {
            var cmd = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "   ", "", "\t" },
                RejectReason = "reason"
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "The question code list cannot be empty.");
        }

        [Fact]
        public void Validator_Should_Fail_When_Ids_Duplicate_AfterTrim()
        {
            var cmd = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { " qb-01 ", "qb-01" },
                RejectReason = "reason"
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "List of duplicate question codes.");
        }

        [Fact]
        public void Validator_Should_Fail_When_RejectReason_Empty()
        {
            var cmd = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "qb-01" },
                RejectReason = ""
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeFalse();
            // WithName => message default của FluentValidation có thể thay đổi theo culture,
            // nên assert theo PropertyName là bền hơn.
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RejectQuestionBanksCommand.RejectReason));
        }

        [Fact]
        public void Validator_Should_Pass_When_Valid()
        {
            var cmd = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { " qb-01 ", "qb-02" },
                RejectReason = "There is a content error"
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeTrue();
        }
    }
}
