using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.RejectQuestionBank;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands.RejectQuestionBank
{
    public class RejectQuestionBankCommandHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _qbRepoMock = new();
        private readonly Mock<IAccountRepository> _accRepoMock = new();
        private readonly Mock<IEmailService> _emailMock = new();
        private readonly Mock<IHttpContextAccessor> _httpMock = new();
        private readonly Mock<ILogger<RejectQuestionBanksCommandHandler>> _loggerMock = new();

        public RejectQuestionBankCommandHandlerTests()
        {
            // Default HTTP Context with user
            SetupHttpContext("admin-id");
        }

        private void SetupHttpContext(string? userId)
        {
            if (userId == null)
            {
                _httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
                return;
            }

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            var context = new DefaultHttpContext { User = principal };
            _httpMock.Setup(x => x.HttpContext).Returns(context);
        }

        private RejectQuestionBanksCommandHandler CreateHandler()
            => new(_qbRepoMock.Object, _accRepoMock.Object, _emailMock.Object, _httpMock.Object, _loggerMock.Object);

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-01 | A | Unauthorized -> 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            SetupHttpContext(null);
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "qb-1" }, RejectReason = "bad" };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup = "RejectQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-REJ-01",
                Description = "Returns error if user is unauthorized",
                ExpectedResult = "Return 401 Unauthorized",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "currentUserId is empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-02 | A | Empty IDs -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyIds_ShouldReturn400()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string>(), RejectReason = "bad" };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup = "RejectQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-REJ-02",
                Description = "Returns error if QuestionBankIds is empty",
                ExpectedResult = "Return 400 ValidationFailed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ids loop returns 0 count" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-03 | A | Empty Reason -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyReason_ShouldReturn400()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "qb-1" }, RejectReason = "" };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors[0].Code.Should().Be("REJECT_REASON_REQUIRED");

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup = "RejectQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-REJ-03",
                Description = "Returns error if RejectReason is empty at handler level",
                ExpectedResult = "Return 400 ValidationFailed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "RejectReason is white space" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-04 | A | NotFound ID -> 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MissingId_ShouldReturn404()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "qb-1" }, RejectReason = "bad" };
            _qbRepoMock.Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<QuestionBank>()); // Return empty list == not found
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup = "RejectQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-REJ-04",
                Description = "Returns error if ID is not found in db",
                ExpectedResult = "Return 404",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Missing IDs in GetByIdsWithDetailsAsync" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-05 | A | Status Deleted -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DeletedStatus_ShouldReturn400()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "qb-1" }, RejectReason = "bad" };
            _qbRepoMock.Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<QuestionBank> { new QuestionBank { QuestionBankId = "qb-1", Status = QuestionBankStatus.Deleted } });
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors[0].Description.Should().Contain("đã bị xóa");

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup = "RejectQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-REJ-05",
                Description = "Blocks if the bank is already Deleted",
                ExpectedResult = "Return 400",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status == Deleted" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-06 | A | Status Not PendingApproval -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NotPendingStatus_ShouldReturn400()
        {
            // Already Rejected shouldn't trigger error, it just skips, so we use Draft (which is != PendingApproval and != Rejected)
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "qb-1" }, RejectReason = "bad" };
            _qbRepoMock.Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<QuestionBank> { new QuestionBank { QuestionBankId = "qb-1", Status = QuestionBankStatus.Draft } });
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup = "RejectQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-REJ-06",
                Description = "Blocks if the bank is Not PendingApproval",
                ExpectedResult = "Return 400 ValidationFailed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status != PendingApproval" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-07 | N | Success -> 200 Send Email
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Success_ShouldReturn200AndSendEmail()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "qb-1", "qb-2" }, RejectReason = "bad formatting" };
            var qb1 = new QuestionBank 
            { 
                QuestionBankId = "qb-1", 
                Status = QuestionBankStatus.PendingApproval, 
                CreateBy = "creator-1" 
            };
            var qb2 = new QuestionBank 
            { 
                QuestionBankId = "qb-2", 
                Status = QuestionBankStatus.Rejected, // Idempotent check
                CreateBy = "creator-1" 
            };

            _qbRepoMock.Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<QuestionBank> { qb1, qb2 });
            _accRepoMock.Setup(x => x.GetByIdAsync("creator-1"))
                        .ReturnsAsync(new Account { UserId = "creator-1", Email = "staff@tokki.com", FullName = "Staff" });

            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            _qbRepoMock.Verify(x => x.UpdateRangeAsync(It.Is<List<QuestionBank>>(l => l.Contains(qb1) && l.Count == 1)), Times.Once);
            _qbRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            
            _emailMock.Verify(x => x.SendEmailAsync("staff@tokki.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup = "RejectQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-REJ-07",
                Description = "Rejects pending QA correctly and sends email",
                ExpectedResult = "Return 200 and sent email",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid flow with email sending" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-08 | E | Exception Thrown -> 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Exception_ShouldReturn500()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "qb-1" }, RejectReason = "bad" };
            _qbRepoMock.Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception("DB Down"));
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup = "RejectQuestionBanksCommandHandler",
                TestCaseID = "TC-QB-REJ-08",
                Description = "Returns error if exception is thrown",
                ExpectedResult = "Return 500 ServerError",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DB throws exception" }
            });
        }
    }
}
