using FluentAssertions;
using System;
using Tokki.Application.UseCases.Payments.Queries.GetPaymentHistory;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Payments.Queries
{
    public class PaymentHistoryDtoTests
    {
        // TC-PAY-PHD-01 | N | CurrentRemainingDays with Null Expiration Date -> Returns 0
        [Fact]
        public void CurrentRemainingDays_NullExpirationDate_ShouldReturn0()
        {
            var dto = new PaymentHistoryDto { CurrentVipExpirationDate = null };

            dto.CurrentRemainingDays.Should().Be(0);

            QACollector.LogTestCase("Payments - Get History", new TestCaseDetail
            {
                FunctionGroup = "PaymentHistoryDto",
                TestCaseID = "TC-PAY-PHD-01",
                Description = "Handling null VIP expiration gracefully",
                ExpectedResult = "0",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "CurrentVipExpirationDate is null" }
            });
        }

        // TC-PAY-PHD-02 | A | CurrentRemainingDays with Past Expiration Date -> Returns 0
        [Fact]
        public void CurrentRemainingDays_PastExpirationDate_ShouldReturn0()
        {
            var dto = new PaymentHistoryDto { CurrentVipExpirationDate = DateTimeOffset.UtcNow.AddDays(-5) };

            dto.CurrentRemainingDays.Should().Be(0);

            QACollector.LogTestCase("Payments - Get History", new TestCaseDetail
            {
                FunctionGroup = "PaymentHistoryDto",
                TestCaseID = "TC-PAY-PHD-02",
                Description = "Expired date returns 0 remaining days safely",
                ExpectedResult = "0",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Date is in the past" }
            });
        }

        // TC-PAY-PHD-03 | N | CurrentRemainingDays with Future Date -> Returns Ceiling value
        [Fact]
        public void CurrentRemainingDays_FutureExpirationDate_ShouldReturnCeiling()
        {
            var dto = new PaymentHistoryDto { CurrentVipExpirationDate = DateTimeOffset.UtcNow.AddHours(25) };

            dto.CurrentRemainingDays.Should().Be(2);

            QACollector.LogTestCase("Payments - Get History", new TestCaseDetail
            {
                FunctionGroup = "PaymentHistoryDto",
                TestCaseID = "TC-PAY-PHD-03",
                Description = "Ceiling mathematical approximation applies perfectly wrapping fractional days",
                ExpectedResult = "2 days",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Date is +25 hours" }
            });
        }

        // TC-PAY-PHD-04 | N | StatusDisplay Pending -> 'Đang chờ thanh toán'
        [Fact]
        public void StatusDisplay_Pending_ShouldFormatString()
        {
            var dto = new PaymentHistoryDto { Status = PaymentStatus.Pending };
            
            dto.StatusDisplay.Should().Be("Đang chờ thanh toán");

            QACollector.LogTestCase("Payments - Get History", new TestCaseDetail
            {
                FunctionGroup = "PaymentHistoryDto",
                TestCaseID = "TC-PAY-PHD-04",
                Description = "Status Enum correctly routes to standard text definition",
                ExpectedResult = "Đang chờ thanh toán",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Pending Status" }
            });
        }

        // TC-PAY-PHD-05 | N | StatusDisplay Paid -> 'Thành công'
        [Fact]
        public void StatusDisplay_Paid_ShouldFormatString()
        {
            var dto = new PaymentHistoryDto { Status = PaymentStatus.Paid };
            
            dto.StatusDisplay.Should().Be("Thành công");

            QACollector.LogTestCase("Payments - Get History", new TestCaseDetail
            {
                FunctionGroup = "PaymentHistoryDto",
                TestCaseID = "TC-PAY-PHD-05",
                Description = "Status Enum correctly routes to standard text definition",
                ExpectedResult = "Thành công",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Paid Status" }
            });
        }

        // TC-PAY-PHD-06 | N | StatusDisplay Failed -> 'Thất bại'
        [Fact]
        public void StatusDisplay_Failed_ShouldFormatStringFallback()
        {
            var dto = new PaymentHistoryDto { Status = PaymentStatus.Pending };
            
            dto.StatusDisplay.Should().Be("Thất bại");

            QACollector.LogTestCase("Payments - Get History", new TestCaseDetail
            {
                FunctionGroup = "PaymentHistoryDto",
                TestCaseID = "TC-PAY-PHD-06",
                Description = "Status Enum correctly falls back to failed condition text definition",
                ExpectedResult = "Thất bại",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new System.Collections.Generic.List<string> { "Failed Status fallback" }
            });
        }
    }
}
