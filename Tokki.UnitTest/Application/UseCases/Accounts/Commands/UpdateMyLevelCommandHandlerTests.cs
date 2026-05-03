using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.UpdateMyLevel;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts.Commands
{
    public class UpdateMyLevelCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _accountRepoMock = new();

        private UpdateMyLevelCommandHandler CreateHandler()
        {
            return new UpdateMyLevelCommandHandler(_accountRepoMock.Object);
        }

        // UpdateMyLevelCommandHandler_01 | A | User Not Found -> 404
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturn404()
        {
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync((Account?)null);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new UpdateMyLevelCommand { UserId = "u1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Account - Update My Level", new TestCaseDetail
            {
                FunctionGroup = "UpdateMyLevelCommandHandler",
                TestCaseID = "UpdateMyLevelCommandHandler_01",
                Description = "User not found returns 404",
                ExpectedResult = "404 UserNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync -> null" }
            });
        }

        // UpdateMyLevelCommandHandler_02 | A | Inactive User -> 403
        [Fact]
        public async Task Handle_InactiveUser_ShouldReturn403()
        {
            var user = new Account { UserId = "u1", Status = AccountStatus.Inactive };
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new UpdateMyLevelCommand { UserId = "u1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors.First().Code.Should().Be("Account.InActive");

            QACollector.LogTestCase("Account - Update My Level", new TestCaseDetail
            {
                FunctionGroup = "UpdateMyLevelCommandHandler",
                TestCaseID = "UpdateMyLevelCommandHandler_02",
                Description = "Inactive user fails checks",
                ExpectedResult = "403 AccountInActive",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Inactive" }
            });
        }

        // UpdateMyLevelCommandHandler_03 | A | Banned User -> 403
        [Fact]
        public async Task Handle_BannedUser_ShouldReturn403()
        {
            var user = new Account { UserId = "u1", Status = AccountStatus.Banned };
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new UpdateMyLevelCommand { UserId = "u1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors.First().Code.Should().Be("Account.Banned");

            QACollector.LogTestCase("Account - Update My Level", new TestCaseDetail
            {
                FunctionGroup = "UpdateMyLevelCommandHandler",
                TestCaseID = "UpdateMyLevelCommandHandler_03",
                Description = "Banned user fails checks",
                ExpectedResult = "403 AccountBanned",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Banned" }
            });
        }

        // UpdateMyLevelCommandHandler_04 | A | Locked User -> 403
        [Fact]
        public async Task Handle_LockedUser_ShouldReturn403()
        {
            var user = new Account { UserId = "u1", Status = AccountStatus.Active, LockedUntil = DateTime.UtcNow.AddMinutes(30) };
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new UpdateMyLevelCommand { UserId = "u1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors.First().Code.Should().Be("Account.Locked");

            QACollector.LogTestCase("Account - Update My Level", new TestCaseDetail
            {
                FunctionGroup = "UpdateMyLevelCommandHandler",
                TestCaseID = "UpdateMyLevelCommandHandler_04",
                Description = "Locked user fails checks",
                ExpectedResult = "403 AccountLocked",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "LockedUntil > UtcNow" }
            });
        }

        // UpdateMyLevelCommandHandler_05 | A | Invalid Level Enum Cast -> 400
        [Fact]
        public async Task Handle_InvalidLevelEnum_ShouldReturn400()
        {
            var user = new Account { UserId = "u1", Status = AccountStatus.Active };
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new UpdateMyLevelCommand { UserId = "u1", Level = (Tokki.Domain.Enums.TopicLevel)9999 }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Account - Update My Level", new TestCaseDetail
            {
                FunctionGroup = "UpdateMyLevelCommandHandler",
                TestCaseID = "UpdateMyLevelCommandHandler_05",
                Description = "Fails inner handler validation if valid int violates enum constraints",
                ExpectedResult = "400 Invalid Level",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Level not defined in enum" }
            });
        }

        // UpdateMyLevelCommandHandler_06 | N | Valid Level Mapping -> 200
        [Fact]
        public async Task Handle_ValidLevel_ShouldUpdateLevelAndReturn200()
        {
            var user = new Account { UserId = "u1", Status = AccountStatus.Active };
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new UpdateMyLevelCommand { UserId = "u1", Level = Tokki.Domain.Enums.TopicLevel.Level1 }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            user.Level.Should().Be((TopicLevel)1);

            _accountRepoMock.Verify(x => x.UpdateUserAsync(It.IsAny<Account>()), Times.Once);

            QACollector.LogTestCase("Account - Update My Level", new TestCaseDetail
            {
                FunctionGroup = "UpdateMyLevelCommandHandler",
                TestCaseID = "UpdateMyLevelCommandHandler_06",
                Description = "Succesfully changes TopicLevel",
                ExpectedResult = "200 Success + Level casted",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Level is valid enum bounds" }
            });
        }

        // UpdateMyLevelCommandHandler_07 | N | Null Level Mapping -> Sets Level to null -> 200
        [Fact]
        public async Task Handle_NullLevel_ShouldSetLevelToNullAndReturn200()
        {
            var user = new Account { UserId = "u1", Status = AccountStatus.Active, Level = TopicLevel.Level1 };
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new UpdateMyLevelCommand { UserId = "u1", Level = null }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            user.Level.Should().BeNull();

            _accountRepoMock.Verify(x => x.UpdateUserAsync(It.IsAny<Account>()), Times.Once);

            QACollector.LogTestCase("Account - Update My Level", new TestCaseDetail
            {
                FunctionGroup = "UpdateMyLevelCommandHandler",
                TestCaseID = "UpdateMyLevelCommandHandler_07",
                Description = "Allows removing a level completely",
                ExpectedResult = "200 Success + Level becomes null",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Level = null" }
            });
        }
    }
}
