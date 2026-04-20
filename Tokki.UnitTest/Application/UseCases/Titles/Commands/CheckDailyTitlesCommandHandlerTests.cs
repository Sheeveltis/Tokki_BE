using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Titles.Commands.CheckDailyTitles;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Titles.Commands
{
    public class CheckDailyTitlesCommandHandlerTests
    {
        private readonly Mock<IUserTitleService> _mockService;
        private readonly CheckDailyTitlesCommandHandler _handler;

        public CheckDailyTitlesCommandHandlerTests()
        {
            _mockService = new Mock<IUserTitleService>();
            _handler = new CheckDailyTitlesCommandHandler(_mockService.Object);
        }

        [Fact]
        public async Task Handle_NoNewTitles_ReturnsSuccessWithEmptyMessage()
        {
            _mockService.Setup(x => x.CheckAndUnlockDailyTitlesAsync("u1"))
                        .ReturnsAsync(new List<Title>());

            var result = await _handler.Handle(new CheckDailyTitlesCommand { UserId = "u1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Không có danh hiệu hàng ngày mới được mở khóa.");
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("Title - Check Daily", new TestCaseDetail
            {
                FunctionGroup     = "CheckDailyTitlesCommandHandler",
                TestCaseID        = "CheckDailyTitlesCommandHandler_01",
                Description       = "No new titles unlocked",
                ExpectedResult    = "200 Success with specific message",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Returns empty list", "Verifies message" }
            });
        }

        [Fact]
        public async Task Handle_NewTitlesUnlocked_ReturnsSuccessWithCongratMessage()
        {
            var unlockList = new List<Title> { new Title { Name = "First Blood" } };
            _mockService.Setup(x => x.CheckAndUnlockDailyTitlesAsync("u1"))
                        .ReturnsAsync(unlockList);

            var result = await _handler.Handle(new CheckDailyTitlesCommand { UserId = "u1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Contain("Chúc mừng! Bạn đã mở khóa 1 danh hiệu");
            result.Data.Should().HaveCount(1);

            QACollector.LogTestCase("Title - Check Daily", new TestCaseDetail
            {
                FunctionGroup     = "CheckDailyTitlesCommandHandler",
                TestCaseID        = "CheckDailyTitlesCommandHandler_02",
                Description       = "Titles unlocked successfully",
                ExpectedResult    = "200 Success with congratulatory message",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Returns populated list", "Verifies success message" }
            });
        }
    }
}
