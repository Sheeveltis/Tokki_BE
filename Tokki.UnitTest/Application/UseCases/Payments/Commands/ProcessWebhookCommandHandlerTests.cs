using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Payments.Commands.ProcessWebhook;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Payments.Commands
{
    public class ProcessWebhookCommandHandlerTests
    {
        private readonly Mock<IPaymentRepository> _payMock = new();
        private readonly Mock<IAccountRepository> _accMock = new();
        private readonly Mock<IVipPackageRepository> _vipMock = new();
        private readonly Mock<IIdGeneratorService> _idMock = new();
        private readonly Mock<ILogger<ProcessWebhookCommandHandler>> _logMock = new();

        private ProcessWebhookCommandHandler CreateHandler()
        {
            return new ProcessWebhookCommandHandler(_payMock.Object, _accMock.Object, _vipMock.Object, _idMock.Object, _logMock.Object);
        }

        // -----------------------------------------------------------
        // ProcessWebhookCommandHandler_01 | A | Unable To Extract PaymentId RegEx
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_NoPaymentIdInContent_ShouldReturnSuccessWithMessage()
        {
            var handler = CreateHandler();
            var cmd = new ProcessWebhookCommand(new SePayWebhookData { Content ="Transfer Random Text Valid", TransactionDate = DateTime.Now.ToString("O") });

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain(Tokki.Application.Common.Models.AppErrors.PaymentInvalidContent.Description);

            QACollector.LogTestCase("Payments - Webhook", new TestCaseDetail
            {
                FunctionGroup ="ProcessWebhookCommandHandler",
                TestCaseID ="ProcessWebhookCommandHandler_01",
                Description ="Regex prevents invalid garbage",
                ExpectedResult ="Success wrap 200 with Warning Invalid Content message smoothly",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"String misses valid Regex 10 chars limits" }
            });
        }

        // -----------------------------------------------------------
        // ProcessWebhookCommandHandler_02 | A | Payment ID Extracted But Not Found -> 200
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_PaymentNotFound_ShouldReturnSuccessWithWarning()
        {
            _payMock.Setup(x => x.GetByIdAsync("ABCDEFGHIJ")).ReturnsAsync((Payment?)null);
            var handler = CreateHandler();
            var cmd = new ProcessWebhookCommand(new SePayWebhookData { Content ="CK cho ABCDEFGHIJ ok", TransactionDate = DateTime.Now.ToString("O") });

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain(Tokki.Application.Common.Models.AppErrors.PaymentNotFound.Description);

            QACollector.LogTestCase("Payments - Webhook", new TestCaseDetail
            {
                FunctionGroup ="ProcessWebhookCommandHandler",
                TestCaseID ="ProcessWebhookCommandHandler_02",
                Description ="Safely captures IDs natively but blocks null Database entity responses",
                ExpectedResult ="Success mapping Not Found Error object safely",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Extracted Id is 10 valid chars but mapped missing" }
            });
        }

        // -----------------------------------------------------------
        // ProcessWebhookCommandHandler_03 | N | Payment Already Processed Status
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_AlreadyProcessed_ShouldReturnAppropriateMessage()
        {
            var p = new Payment { Status = PaymentStatus.Paid };
            _payMock.Setup(x => x.GetByIdAsync("ABCDEFGHIJ")).ReturnsAsync(p);
            var handler = CreateHandler();
            var cmd = new ProcessWebhookCommand(new SePayWebhookData { Content ="CK cho ABCDEFGHIJ ok", TransactionDate = DateTime.Now.ToString("O") });

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain(Tokki.Application.Common.Models.AppErrors.PaymentAlreadyProcessed.Description);

            QACollector.LogTestCase("Payments - Webhook", new TestCaseDetail
            {
                FunctionGroup ="ProcessWebhookCommandHandler",
                TestCaseID ="ProcessWebhookCommandHandler_03",
                Description ="Prevents multi webhook concurrent execution overwrites efficiently organically successfully",
                ExpectedResult ="Already Processed safely checked natively",
                StatusRound1 ="Passed",
                TestCaseType ="N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Status != Pending properly" }
            });
        }

        // -----------------------------------------------------------
        // ProcessWebhookCommandHandler_04 | A | Insufficient Transfer Amount
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_InsufficientAmount_ShouldReturnMessage()
        {
            var p = new Payment { Status = PaymentStatus.Pending, Amount = 100000 };
            _payMock.Setup(x => x.GetByIdAsync("ABCDEFGHIJ")).ReturnsAsync(p);
            var handler = CreateHandler();
            var cmd = new ProcessWebhookCommand(new SePayWebhookData { Content ="CK ABCDEFGHIJ", TransferAmount = 50000, TransactionDate = DateTime.Now.ToString("O") });

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain("không d? so v?i hóa don");

            QACollector.LogTestCase("Payments - Webhook", new TestCaseDetail
            {
                FunctionGroup ="ProcessWebhookCommandHandler",
                TestCaseID ="ProcessWebhookCommandHandler_04",
                Description ="Accurately bounds logic ensuring transactions validate complete limits accurately securely securely",
                ExpectedResult ="Insufficient Amount warning mapped securely",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"TransferAmount < PaymentAmount" }
            });
        }
        
        // -----------------------------------------------------------
        // ProcessWebhookCommandHandler_05 | A | Missing VIP Package Limits Setup
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_MissingVipPackage_ShouldReturnFailure()
        {
            var p = new Payment { Status = PaymentStatus.Pending, Amount = 100000, VipPackageId ="pkg" };
            _payMock.Setup(x => x.GetByIdAsync("ABCDEFGHIJ")).ReturnsAsync(p);
            _vipMock.Setup(x => x.GetByIdAsync("pkg")).ReturnsAsync((VipPackage?)null);
            
            var handler = CreateHandler();
            var cmd = new ProcessWebhookCommand(new SePayWebhookData { Content ="CK ABCDEFGHIJ", TransferAmount = 100000, TransactionDate = DateTime.Now.ToString("O") });

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain(Tokki.Application.Common.Models.AppErrors.VipPackageNotFound.Description);

            QACollector.LogTestCase("Payments - Webhook", new TestCaseDetail
            {
                FunctionGroup ="ProcessWebhookCommandHandler",
                TestCaseID ="ProcessWebhookCommandHandler_05",
                Description ="Failures matching deleted Packages abort upgrade",
                ExpectedResult ="Return Vip NotFound Error explicitly explicitly",
                StatusRound1 ="Passed",
                TestCaseType ="A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Valid Payment but bound VIP pkg missing mapped logic" }
            });
        }

        // -----------------------------------------------------------
        // ProcessWebhookCommandHandler_06 | N | Success Mapping Extends Expiration
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_SuccessValid_ShouldUpdateUserAndPayment()
        {
            var p = new Payment { Status = PaymentStatus.Pending, Amount = 100000, VipPackageId ="pkg", UserId ="usr" };
            var vip = new VipPackage { DurationDays = 30 };
            var u = new Account { UserId ="usr", Role = AccountRole.User }; // Non Vip Upgrading

            _payMock.Setup(x => x.GetByIdAsync("ABCDEFGHIJ")).ReturnsAsync(p);
            _vipMock.Setup(x => x.GetByIdAsync("pkg")).ReturnsAsync(vip);
            _accMock.Setup(x => x.GetByIdAsync("usr")).ReturnsAsync(u);

            var handler = CreateHandler();
            var cmd = new ProcessWebhookCommand(new SePayWebhookData { Content ="CK ABCDEFGHIJ ok", TransferType ="in", TransferAmount = 150000, TransactionDate = DateTime.Now.ToString("O") });

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain("Thanh toán thành công");

            u.Role.Should().Be(AccountRole.Vip);
            u.VipExpirationDate.Should().HaveValue();
            
            _payMock.Verify(x => x.UpdateAsync(It.Is<Payment>(y => y.Status == PaymentStatus.Paid)), Times.Once);
            _accMock.Verify(x => x.UpdateUserAsync(u), Times.Once);

            QACollector.LogTestCase("Payments - Webhook", new TestCaseDetail
            {
                FunctionGroup ="ProcessWebhookCommandHandler",
                TestCaseID ="ProcessWebhookCommandHandler_06",
                Description ="Extends role successfully validating math boundaries",
                ExpectedResult ="Status updated Paid Roles updated",
                StatusRound1 ="Passed",
                TestCaseType ="N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Successfully updates user" }
            });
        }
    }
}
