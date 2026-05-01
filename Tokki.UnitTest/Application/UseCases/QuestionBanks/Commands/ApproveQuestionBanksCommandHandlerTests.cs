using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.ApproveQuestionBank;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands
{
    public class ApproveQuestionBanksCommandHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _qbMock = new();
        private readonly Mock<IAccountRepository> _accMock = new();
        private readonly Mock<IEmailService> _emailMock = new();
        private readonly Mock<IHttpContextAccessor> _httpMock = new();
        private readonly Mock<ILogger<ApproveQuestionBanksCommandHandler>> _logMock = new();

        private ApproveQuestionBanksCommandHandler CreateHandler()
        {
            return new ApproveQuestionBanksCommandHandler(_qbMock.Object, _accMock.Object, _emailMock.Object, _httpMock.Object, _logMock.Object);
        }

        private void SetupHttpContext(string? userId)
        {
            if (userId == null)
            {
                _httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
                return;
            }
            var context = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) });
            context.User = new ClaimsPrincipal(identity);
            _httpMock.Setup(x => x.HttpContext).Returns(context);
        }

        // -----------------------------------------------------------
        // ApproveQuestionBanksCommandHandler_01 | A | Context Null -> 401
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_UserUnauthorized_ShouldReturn401()
        {
            SetupHttpContext(null);
            var handler = CreateHandler();
            var result = await handler.Handle(new ApproveQuestionBanksCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup ="ApproveQuestionBanksCommandHandler",
                TestCaseID ="ApproveQuestionBanksCommandHandler_01",
                Description ="Rejects immediately gracefully missing authentication tokens",
                ExpectedResult ="Return 401 error",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"HTTP Context Null smartly" }
            });
        }

        // -----------------------------------------------------------
        // ApproveQuestionBanksCommandHandler_02 | A | Ids Empty -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_IdsEmpty_ShouldReturn400()
        {
            SetupHttpContext("admin");
            var handler = CreateHandler();
            var result = await handler.Handle(new ApproveQuestionBanksCommand { QuestionBankIds = new List<string>() }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup ="ApproveQuestionBanksCommandHandler",
                TestCaseID ="ApproveQuestionBanksCommandHandler_02",
                Description ="Empty arrays correctly mapped",
                ExpectedResult ="Return 400 perfectly",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"List is Empty" }
            });
        }

        // -----------------------------------------------------------
        // ApproveQuestionBanksCommandHandler_03 | A | Missing Ids match mapped -> 404
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_MissingIds_ShouldReturn404()
        {
            SetupHttpContext("admin");
            _qbMock.Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuestionBank>());
            
            var handler = CreateHandler();
            var result = await handler.Handle(new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> {"fake1" } }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup ="ApproveQuestionBanksCommandHandler",
                TestCaseID ="ApproveQuestionBanksCommandHandler_03",
                Description ="Verification properly matches repo",
                ExpectedResult ="Return 404",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Unmapped ID" }
            });
        }

        // -----------------------------------------------------------
        // ApproveQuestionBanksCommandHandler_04 | A | QB Deleted -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_AlreadyDeleted_ShouldReturn400()
        {
            SetupHttpContext("admin");
            var qb = new QuestionBank { QuestionBankId ="q1", Status = QuestionBankStatus.Deleted };
            _qbMock.Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuestionBank> { qb });
            var handler = CreateHandler();
            var result = await handler.Handle(new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> {"q1" } }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("dã b? xóa");

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup ="ApproveQuestionBanksCommandHandler",
                TestCaseID ="ApproveQuestionBanksCommandHandler_04",
                Description ="Deleted logically denies",
                ExpectedResult ="Return 400",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Status = Deleted" }
            });
        }

        // -----------------------------------------------------------
        // ApproveQuestionBanksCommandHandler_05 | A | Not Pending -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_NotPendingApproval_ShouldReturnValidationFailed()
        {
            SetupHttpContext("admin");
            var qb = new QuestionBank { QuestionBankId ="q1", Status = QuestionBankStatus.Draft };
            _qbMock.Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuestionBank> { qb });
            var handler = CreateHandler();
            var result = await handler.Handle(new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> {"q1" } }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("không ? tr?ng thái PendingApproval");

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup ="ApproveQuestionBanksCommandHandler",
                TestCaseID ="ApproveQuestionBanksCommandHandler_05",
                Description ="Drafts mapped intelligently properly efficiently",
                ExpectedResult ="Return Validation Failed securely",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Status = Draft flawlessly" }
            });
        }

        // -----------------------------------------------------------
        // ApproveQuestionBanksCommandHandler_06 | N | Success -> Sets Status Active and Emits
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_SuccessApproves_ShouldSetStatusAndSave()
        {
            SetupHttpContext("admin");
            var qb = new QuestionBank { QuestionBankId ="q1", Status = QuestionBankStatus.PendingApproval, CreateBy ="u" };
            _qbMock.Setup(x => x.GetByIdsWithDetailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuestionBank> { qb });
            _accMock.Setup(x => x.GetByIdAsync("u")).ReturnsAsync(new Account { Email ="e@a.c" });

            var handler = CreateHandler();
            var result = await handler.Handle(new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> {"q1" } }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain("q1");

            qb.Status.Should().Be(QuestionBankStatus.Active);
            qb.ApprovedBy.Should().Be("admin");

            _qbMock.Verify(x => x.UpdateRangeAsync(It.IsAny<List<QuestionBank>>()), Times.Once);

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup ="ApproveQuestionBanksCommandHandler",
                TestCaseID ="ApproveQuestionBanksCommandHandler_06",
                Description ="Validates active mapping",
                ExpectedResult ="Successfully",
                StatusRound1 ="Passed",
                TestCaseType ="N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Values mapping perfectly" }
            });
        }
    }
}
