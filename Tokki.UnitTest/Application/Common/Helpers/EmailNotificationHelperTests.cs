using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tokki.Application.Common.Helpers;
using Tokki.Application.IServices;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.Common.Helpers
{
    public class EmailNotificationHelperTests
    {
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly EmailNotificationHelper _helper;

        public EmailNotificationHelperTests()
        {
            _mockEmailService = new Mock<IEmailService>();
            _helper = new EmailNotificationHelper(_mockEmailService.Object);
        }

        [Fact]
        public async Task SendContentApprovedAsync_SendsCorrectEmail()
        {
            await _helper.SendContentApprovedAsync("test@test.com", "John", "TitleA", "Topic");

            _mockEmailService.Verify(x => x.SendEmailAsync("test@test.com", 
                "[Tokki] Topic của bạn đã được phê duyệt", 
                It.Is<string>(html => html.Contains("Đã đăng (Published)") && html.Contains("John") && html.Contains("TitleA"))), 
                Times.Once);

            QACollector.LogTestCase("Helper - Email Notification", new TestCaseDetail
            {
                FunctionGroup     = "EmailNotificationHelper",
                TestCaseID        = "EmailNotificationHelper_01",
                Description       = "Content approved email is built and sent correctly",
                ExpectedResult    = "Generates valid HTML and calls send method successfully",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Approved Template" }
            });
        }

        [Fact]
        public async Task SendContentRejectedAsync_WithEmptyName_SendsCorrectEmailWithFallbackName()
        {
            await _helper.SendContentRejectedAsync("test2@test.com", "", "TitleB", "Exam", "Too short");

            _mockEmailService.Verify(x => x.SendEmailAsync("test2@test.com", 
                "[Tokki] Exam của bạn chưa được phê duyệt", 
                It.Is<string>(html => html.Contains("Xin chào Bạn") && html.Contains("Too short") && html.Contains("TitleB"))), 
                Times.Once);

            QACollector.LogTestCase("Helper - Email Notification", new TestCaseDetail
            {
                FunctionGroup     = "EmailNotificationHelper",
                TestCaseID        = "EmailNotificationHelper_02",
                Description       = "Content rejected email fallback empty name",
                ExpectedResult    = "Generates HTML with 'Bạn' instead of name",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Rejected Template", "Empty Fallback Name" }
            });
        }
    }
}
