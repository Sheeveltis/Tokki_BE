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
using Tokki.Application.UseCases.QuestionBanks.Commands.ApproveQuestionBank;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Xunit;

namespace Tokki.UnitTests.Features.QuestionBanks.Commands
{
    public class ApproveQuestionBanksCommandHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _mockQuestionBankRepo = new();
        private readonly Mock<IAccountRepository> _mockAccountRepo = new();
        private readonly Mock<IEmailService> _mockEmailService = new();
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor = new();
        private readonly Mock<ILogger<ApproveQuestionBanksCommandHandler>> _mockLogger = new();

        private ApproveQuestionBanksCommandHandler CreateHandlerWithUser(string? userId)
        {
            var httpContext = new DefaultHttpContext();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                httpContext.User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                        "TestAuth"));
            }
            else
            {
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            }

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            return new ApproveQuestionBanksCommandHandler(
                _mockQuestionBankRepo.Object,
                _mockAccountRepo.Object,
                _mockEmailService.Object,
                _mockHttpContextAccessor.Object,
                _mockLogger.Object
            );
        }

        private static ApproveQuestionBanksCommand BuildCommand(params string[] ids)
        {
            return new ApproveQuestionBanksCommand
            {
                QuestionBankIds = ids?.ToList() ?? new List<string>()
            };
        }

        private static QuestionBank BuildQb(
            string id,
            QuestionBankStatus status,
            string? createBy = null)
        {
            return new QuestionBank
            {
                QuestionBankId = id,
                Status = status,
                CreateBy = createBy,
                ApprovedBy = "old-approver",
                ApprovedDate = DateTime.UtcNow.AddDays(-1),

                // các field nav để Build mail không null (không bắt buộc)
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
        public async Task Handle_Should_ReturnUnauthorized_When_NoUserIdInClaims()
        {
            // Arrange
            var handler = CreateHandlerWithUser(userId: null);
            var command = BuildCommand("qb-01");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            result.Errors.Should().Contain(e => e.Code == AppErrors.UserUnauthorized.Code);

            _mockQuestionBankRepo.Verify(
                x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_IdsEmptyOrInvalid()
        {
            // Arrange
            var handler = CreateHandlerWithUser(userId: "staff-01");
            var command = BuildCommand("   ", "", "\t");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ValidationFailed.Code);
            result.Message.Should().Contain("rỗng hoặc không hợp lệ");

            _mockQuestionBankRepo.Verify(
                x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_MissingIds()
        {
            // Arrange
            var handler = CreateHandlerWithUser(userId: "staff-01");
            var command = BuildCommand("qb-01", "qb-02");

            var qb01 = BuildQb("qb-01", QuestionBankStatus.PendingApproval, createBy: "u1");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdsWithDetailsAsync(
                    It.Is<List<string>>(ids => ids.Count == 2 && ids.Contains("qb-01") && ids.Contains("qb-02")),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuestionBank> { qb01 }); // thiếu qb-02

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(e => e.Code == AppErrors.QuestionBankNotFound.Code);
            result.Message.Should().Contain("qb-02");

            _mockQuestionBankRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<List<QuestionBank>>()), Times.Never);
            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_AnyQbIsDeleted()
        {
            // Arrange
            var handler = CreateHandlerWithUser(userId: "staff-01");
            var command = BuildCommand("qb-01");

            var qb01 = BuildQb("qb-01", QuestionBankStatus.Deleted, createBy: "u1");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuestionBank> { qb01 });

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ValidationFailed.Code);
            result.Message.Should().Contain("đã bị xóa");

            _mockQuestionBankRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<List<QuestionBank>>()), Times.Never);
            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_StatusNotPendingApproval()
        {
            // Arrange
            var handler = CreateHandlerWithUser(userId: "staff-01");
            var command = BuildCommand("qb-01");

            var qb01 = BuildQb("qb-01", QuestionBankStatus.Draft, createBy: "u1");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuestionBank> { qb01 });

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(e => e.Code == AppErrors.ValidationFailed.Code);
            result.Message.Should().Contain("không ở trạng thái PendingApproval");

            _mockQuestionBankRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<List<QuestionBank>>()), Times.Never);
            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_BeIdempotent_When_StatusActive_And_NotSendEmail()
        {
            // Arrange
            var handler = CreateHandlerWithUser(userId: "staff-01");
            var command = BuildCommand("qb-01");

            var qb01 = BuildQb("qb-01", QuestionBankStatus.Active, createBy: "u1");

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuestionBank> { qb01 });

            _mockQuestionBankRepo
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Equal(new List<string> { "qb-01" });

            // Active => không update range
            _mockQuestionBankRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<List<QuestionBank>>()), Times.Never);

            // Handler vẫn SaveChangesAsync
            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Active => không mail lại
            _mockAccountRepo.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ApprovePendingApproval_And_SendOneEmail_PerCreator()
        {
            // Arrange
            var handler = CreateHandlerWithUser(userId: "staff-01");
            var command = BuildCommand("qb-01", "qb-02", "qb-03");

            var qb01 = BuildQb("qb-01", QuestionBankStatus.PendingApproval, createBy: "u1");
            var qb02 = BuildQb("qb-02", QuestionBankStatus.PendingApproval, createBy: "u1");
            var qb03 = BuildQb("qb-03", QuestionBankStatus.Active, createBy: "u2"); // idempotent

            _mockQuestionBankRepo
                .Setup(x => x.GetByIdsWithDetailsAsync(
                    It.IsAny<List<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuestionBank> { qb01, qb02, qb03 });

            _mockQuestionBankRepo
                .Setup(x => x.UpdateRangeAsync(It.IsAny<List<QuestionBank>>()))
                .Returns(Task.CompletedTask);

            _mockQuestionBankRepo
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockAccountRepo
                .Setup(x => x.GetByIdAsync("u1"))
                .ReturnsAsync(new Account
                {
                    UserId = "u1",
                    Email = "u1@mail.com",
                    FullName = "User One"
                });

            _mockEmailService
                .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // idempotent Active vẫn trả về trong list
            result.Data.Should().Equal(new List<string> { "qb-01", "qb-02", "qb-03" });

            // qb01/qb02 được approve
            qb01.Status.Should().Be(QuestionBankStatus.Active);
            qb01.ApprovedBy.Should().Be("staff-01");
            qb01.ApprovedDate.Should().NotBeNull();

            qb02.Status.Should().Be(QuestionBankStatus.Active);
            qb02.ApprovedBy.Should().Be("staff-01");
            qb02.ApprovedDate.Should().NotBeNull();

            // qb03 active giữ nguyên ApprovedBy/ApprovedDate cũ (handler không đụng)
            qb03.Status.Should().Be(QuestionBankStatus.Active);

            // UpdateRange chỉ gồm những qb PendingApproval
            _mockQuestionBankRepo.Verify(
                x => x.UpdateRangeAsync(It.Is<List<QuestionBank>>(list =>
                    list.Count == 2 &&
                    list.Any(q => q.QuestionBankId == "qb-01") &&
                    list.Any(q => q.QuestionBankId == "qb-02"))),
                Times.Once);

            _mockQuestionBankRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Email: gom theo CreateBy => u1 chỉ 1 email
            _mockAccountRepo.Verify(x => x.GetByIdAsync("u1"), Times.Once);
            _mockEmailService.Verify(
                x => x.SendEmailAsync(
                    "u1@mail.com",
                    It.Is<string>(s => s.Contains("Tokki") && s.Contains("phê duyệt")),
                    It.Is<string>(body => body.Contains("qb-01") && body.Contains("qb-02"))),
                Times.Once);

            // u2 không được mail vì qb-03 không approve mới trong batch
            _mockAccountRepo.Verify(x => x.GetByIdAsync("u2"), Times.Never);
        }
    }

    public class ApproveQuestionBanksCommandValidatorTests
    {
        private readonly ApproveQuestionBanksCommandValidator _validator = new();

        [Fact]
        public void Validator_Should_Fail_When_QuestionBankIds_EmptyList()
        {
            var cmd = new ApproveQuestionBanksCommand
            {
                QuestionBankIds = new List<string>()
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "Danh sách mã câu hỏi không được rỗng.");
        }

        [Fact]
        public void Validator_Should_Fail_When_AllWhitespace()
        {
            var cmd = new ApproveQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "   ", "", "\t" }
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "Danh sách mã câu hỏi không được rỗng.");
        }

        [Fact]
        public void Validator_Should_Fail_When_Duplicate_AfterTrim()
        {
            var cmd = new ApproveQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { " qb-01 ", "qb-01" }
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "Danh sách mã câu hỏi bị trùng.");
        }

        [Fact]
        public void Validator_Should_Pass_When_Unique_AfterTrim()
        {
            var cmd = new ApproveQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { " qb-01 ", "qb-02" }
            };

            var result = _validator.Validate(cmd);

            result.IsValid.Should().BeTrue();
        }

        /*
         * Lưu ý kỹ thuật:
         * Validator hiện tại có chain:
         *   NotNull().WithMessage(...)
         *   Must(ids => ids.Any(...)).WithMessage(...)
         *
         * Nếu QuestionBankIds = null, rule Must(ids => ids.Any(...)) có thể throw
         * tùy CascadeMode. Trong thực tế property default = new(), nên test null thường không cần.
         * Nếu bạn muốn validator xử lý null an toàn, nên thêm .Cascade(CascadeMode.Stop)
         * hoặc .When(x => x.QuestionBankIds != null) cho rule Must.
         */
    }
}
