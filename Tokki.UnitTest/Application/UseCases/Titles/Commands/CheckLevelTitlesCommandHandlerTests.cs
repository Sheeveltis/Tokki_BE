using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Titles.Commands.CheckLevelTitles;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Titles.Commands
{
    public class CheckLevelTitlesCommandHandlerTests
    {
        private readonly Mock<IUserTitleService> _mockService;
        private readonly CheckLevelTitlesCommandHandler _handler;

        public CheckLevelTitlesCommandHandlerTests()
        {
            _mockService = new Mock<IUserTitleService>();
            _handler = new CheckLevelTitlesCommandHandler(_mockService.Object);
        }

        [Fact]
        public async Task Handle_NoNewTitles_ReturnsSuccessWithEmptyMessage()
        {
            _mockService.Setup(x => x.CheckAndUnlockLevelTitlesAsync("u1"))
                        .ReturnsAsync(new List<Title>());

            var result = await _handler.Handle(new CheckLevelTitlesCommand { UserId = "u1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Không có danh hiệu cấp độ mới được mở khóa.");
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("Title - Check Level", new TestCaseDetail
            {
                FunctionGroup     = "CheckLevelTitlesCommandHandler",
                TestCaseID        = "CheckLevelTitlesCommandHandler_01",
                Description       = "No level titles unlocked",
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
            var unlockList = new List<Title> { new Title { Name = "Master" } };
            _mockService.Setup(x => x.CheckAndUnlockLevelTitlesAsync("u1"))
                        .ReturnsAsync(unlockList);

            var result = await _handler.Handle(new CheckLevelTitlesCommand { UserId = "u1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Contain("Chúc mừng! Bạn đã mở khóa 1 danh hiệu cấp độ mới.");
            result.Data.Should().HaveCount(1);

            QACollector.LogTestCase("Title - Check Level", new TestCaseDetail
            {
                FunctionGroup     = "CheckLevelTitlesCommandHandler",
                TestCaseID        = "CheckLevelTitlesCommandHandler_02",
                Description       = "Level titles unlocked successfully",
                ExpectedResult    = "200 Success with congratulatory message",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Returns populated list", "Verifies success message" }
            });
        }
    }
}
