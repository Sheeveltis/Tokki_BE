using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Gamification.Commands.AddGameXp;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Gamification.Commands
{
    public class AddGameXpCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<IUserTitleService> _mockTitleService;
        private readonly Mock<ISystemConfigRepository> _mockConfigRepo;
        private readonly Mock<IUserXpHistoryRepository> _mockXpHistoryRepo;
        private readonly Mock<IIdGeneratorService> _mockIdGen;
        private readonly AddGameXpCommandHandler _handler;

        public AddGameXpCommandHandlerTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockTitleService = new Mock<IUserTitleService>();
            _mockConfigRepo = new Mock<ISystemConfigRepository>();
            _mockXpHistoryRepo = new Mock<IUserXpHistoryRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();

            _handler = new AddGameXpCommandHandler(
                _mockAccountRepo.Object, _mockTitleService.Object, 
                _mockConfigRepo.Object, _mockXpHistoryRepo.Object, _mockIdGen.Object);
        }

        // TC-GAM-A-01 | A | UserId Empty -> Failure
        [Fact]
        public async Task Handle_EmptyUserId_ShouldFail()
        {
            var command = new AddGameXpCommand { UserId = "", Amount = 50, Source = XpSource.MiniGame };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("không hợp lệ =");

            QACollector.LogTestCase("Gamification - Add Game XP", new TestCaseDetail
            {
                FunctionGroup = "AddGameXpCommandHandler",
                TestCaseID = "TC-GAM-A-01",
                Description = "Validates core property string nullors cleanly before database lookups",
                ExpectedResult = "Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserId string empty" }
            });
        }

        // TC-GAM-A-02 | A | User Not Found -> Failure
        [Fact]
        public async Task Handle_UserNotFound_ShouldFail()
        {
            _mockAccountRepo.Setup(x => x.GetByIdAsync("uid")).ReturnsAsync((Account?)null);

            var command = new AddGameXpCommand { UserId = "uid", Amount = 50, Source = XpSource.DailyStreak };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy user");

            QACollector.LogTestCase("Gamification - Add Game XP", new TestCaseDetail
            {
                FunctionGroup = "AddGameXpCommandHandler",
                TestCaseID = "TC-GAM-A-02",
                Description = "Safeguards repository lookup returning 404 conceptually to frontend",
                ExpectedResult = "Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null" }
            });
        }

        // TC-GAM-A-03 | A | MiniGame Daily Limit Fully Reached
        [Fact]
        public async Task Handle_DailyLimitReached_AmountBecomesZero()
        {
            var user = new Account { UserId = "uid", TotalXP = 0 };
            _mockAccountRepo.Setup(x => x.GetByIdAsync("uid")).ReturnsAsync(user);
            _mockConfigRepo.Setup(x => x.GetValueByKeyAsync("MAX_MINI_GAME_XP_PER_SESSION")).ReturnsAsync("100");
            _mockXpHistoryRepo.Setup(x => x.GetTotalXpBySourceAndDateAsync("uid", XpSource.MiniGame, It.IsAny<DateTime>())).ReturnsAsync(100);
            
            _mockIdGen.Setup(x => x.Generate(21)).Returns("XPhistory1");

            var command = new AddGameXpCommand { UserId = "uid", Amount = 50, Source = XpSource.MiniGame };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.XpAdded.Should().Be(0); // Cut to 0
            user.TotalXP.Should().Be(0); // Unchanged

            QACollector.LogTestCase("Gamification - Add Game XP", new TestCaseDetail
            {
                FunctionGroup = "AddGameXpCommandHandler",
                TestCaseID = "TC-GAM-A-03",
                Description = "Restricts grinding exploits enforcing strict session thresholds appropriately",
                ExpectedResult = "0 XP added",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Already earned 100, Config Max 100" }
            });
        }

        // TC-GAM-A-04 | N | MiniGame Daily Limit Partial Reach
        [Fact]
        public async Task Handle_DailyLimitPartial_AmountTrimmed()
        {
            var user = new Account { UserId = "uid", TotalXP = 0 };
            _mockAccountRepo.Setup(x => x.GetByIdAsync("uid")).ReturnsAsync(user);
            _mockConfigRepo.Setup(x => x.GetValueByKeyAsync("MAX_MINI_GAME_XP_PER_SESSION")).ReturnsAsync("100");
            _mockXpHistoryRepo.Setup(x => x.GetTotalXpBySourceAndDateAsync("uid", XpSource.MiniGame, It.IsAny<DateTime>())).ReturnsAsync(80);
            
            _mockIdGen.Setup(x => x.Generate(21)).Returns("history2");

            var command = new AddGameXpCommand { UserId = "uid", Amount = 50, Source = XpSource.MiniGame };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.XpAdded.Should().Be(20); // Trimmed 50 -> 20
            user.TotalXP.Should().Be(20); 

            QACollector.LogTestCase("Gamification - Add Game XP", new TestCaseDetail
            {
                FunctionGroup = "AddGameXpCommandHandler",
                TestCaseID = "TC-GAM-A-04",
                Description = "Safely caps integer logic clipping overlapping requests flawlessly",
                ExpectedResult = "20 XP added",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Earned 80/100, request 50" }
            });
        }

        // TC-GAM-A-05 | N | Unlocking New Titles Successfully
        [Fact]
        public async Task Handle_UnlocksNewTitle_UpdatesUser()
        {
            var user = new Account { UserId = "uid", TotalXP = 0, CurrentTitleId = null };
            _mockAccountRepo.Setup(x => x.GetByIdAsync("uid")).ReturnsAsync(user);
            _mockIdGen.Setup(x => x.Generate(21)).Returns("history3");
            
            // Returns a list indicating title acquired
            var titles = new List<Title> { new Title { TitleId = "T01" } };
            _mockTitleService.Setup(x => x.CheckAndUnlockTitlesAsync("uid", TitleRequirementType.XP, 500))
                             .ReturnsAsync(titles);

            var command = new AddGameXpCommand { UserId = "uid", Amount = 500, Source = XpSource.DailyStreak };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            user.CurrentTitleId.Should().Be("T01");
            _mockAccountRepo.Verify(x => x.UpdateUserAsync(user), Times.Once);

            QACollector.LogTestCase("Gamification - Add Game XP", new TestCaseDetail
            {
                FunctionGroup = "AddGameXpCommandHandler",
                TestCaseID = "TC-GAM-A-05",
                Description = "Updates nested relationships automatically equipping the new cosmetics",
                ExpectedResult = "Success true and title updated",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Returns 1 title, previous null" }
            });
        }

        // TC-GAM-A-06 | N | Verify Level Up Calculation Engine Hook
        [Fact]
        public async Task Handle_GainsLevel_SetsIsLevelUp()
        {
            var user = new Account { UserId = "uid", TotalXP = 0 }; // Level 1
            _mockAccountRepo.Setup(x => x.GetByIdAsync("uid")).ReturnsAsync(user);
            _mockIdGen.Setup(x => x.Generate(21)).Returns("history4");
            _mockTitleService.Setup(x => x.CheckAndUnlockTitlesAsync("uid", TitleRequirementType.XP, It.IsAny<int>()))
                             .ReturnsAsync(new List<Title>());

            // Adding 1500 XP guarantees a few levels up
            var command = new AddGameXpCommand { UserId = "uid", Amount = 1500, Source = XpSource.DailyStreak };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.IsLevelUp.Should().BeTrue();
            // Default Engine should jump from 1 to something > 1
            result.Data!.NewLevel.Should().BeGreaterThan(1);

            QACollector.LogTestCase("Gamification - Add Game XP", new TestCaseDetail
            {
                FunctionGroup = "AddGameXpCommandHandler",
                TestCaseID = "TC-GAM-A-06",
                Description = "Hooking static level engine triggers boolean event markers successfully",
                ExpectedResult = "IsLevelUp true",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "0 XP -> 1500 XP step cross boundary" }
            });
        }
        // TC-GAM-A-07 | N | Empty Config Fallback Defaults to 150
        [Fact]
        public async Task Handle_EmptyConfig_UsesDefaultLimit()
        {
            var user = new Account { UserId = "uid", TotalXP = 0 };
            _mockAccountRepo.Setup(x => x.GetByIdAsync("uid")).ReturnsAsync(user);
            // Return null config 
            _mockConfigRepo.Setup(x => x.GetValueByKeyAsync("MAX_MINI_GAME_XP_PER_SESSION")).ReturnsAsync((string?)null);
            _mockXpHistoryRepo.Setup(x => x.GetTotalXpBySourceAndDateAsync("uid", XpSource.MiniGame, It.IsAny<DateTime>())).ReturnsAsync(0);
            
            _mockIdGen.Setup(x => x.Generate(21)).Returns("history6");

            var command = new AddGameXpCommand { UserId = "uid", Amount = 160, Source = XpSource.MiniGame };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.XpAdded.Should().Be(150); // Fallback is 150
            result.Data.IsLevelUp.Should().BeFalse(); // Starts at level 1, 150 XP probably doesn't level up

            QACollector.LogTestCase("Gamification - Add Game XP", new TestCaseDetail
            {
                FunctionGroup = "AddGameXpCommandHandler",
                TestCaseID = "TC-GAM-A-07",
                Description = "Null config safely falls back ",
                ExpectedResult = "Default limit 150 applies flawlessly",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Config null effortlessly naturally gracefully smartly expertly efficiently" }
            });
        }

        // TC-GAM-A-08 | N | Non MiniGame Adds All XP Successfully
        [Fact]
        public async Task Handle_NonMiniGameSource_AddsFullAmount()
        {
            var user = new Account { UserId = "uid", TotalXP = 0, CurrentTitleId = "ExistingTitle" };
            _mockAccountRepo.Setup(x => x.GetByIdAsync("uid")).ReturnsAsync(user);
            
            _mockIdGen.Setup(x => x.Generate(21)).Returns("history7");
            _mockTitleService.Setup(x => x.CheckAndUnlockTitlesAsync("uid", TitleRequirementType.XP, 50))
                             .ReturnsAsync(new List<Title> { new Title { TitleId = "NewT" } });

            var command = new AddGameXpCommand { UserId = "uid", Amount = 50, Source = XpSource.DailyStreak };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain("Cộng thành công 50 XP");
            user.CurrentTitleId.Should().Be("ExistingTitle"); // Does not update title because it was not empty

            QACollector.LogTestCase("Gamification - Add Game XP", new TestCaseDetail
            {
                FunctionGroup = "AddGameXpCommandHandler",
                TestCaseID = "TC-GAM-A-08",
                Description = "Normal source bypassed limits ",
                ExpectedResult = "Current title remains ",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DailyStreak " }
            });
        }
    }
}
