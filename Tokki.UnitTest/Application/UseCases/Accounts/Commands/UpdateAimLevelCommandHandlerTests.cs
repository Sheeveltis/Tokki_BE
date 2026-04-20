using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Commands.UpdateAimLevel;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts.Commands
{
    public class UpdateAimLevelCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _accountRepoMock = new();

        private UpdateAimLevelCommandHandler CreateHandler()
        {
            return new UpdateAimLevelCommandHandler(_accountRepoMock.Object);
        }

        // UpdateAimLevelCommandHandler_01 | A | UserId is null -> 400
        [Fact]
        public async Task Handle_NullUserId_ShouldReturnFailure400()
        {
            var handler = CreateHandler();
            var result = await handler.Handle(new UpdateAimLevelCommand { UserId = null }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("UserId is required.");

            QACollector.LogTestCase("Account - Update Aim", new TestCaseDetail
            {
                FunctionGroup = "UpdateAimLevelCommandHandler",
                TestCaseID = "UpdateAimLevelCommandHandler_01",
                Description = "Null UserId returns 400",
                ExpectedResult = "400 UserId is required.",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserId = null" }
            });
        }

        // UpdateAimLevelCommandHandler_02 | A | UserId is empty -> 400
        [Fact]
        public async Task Handle_EmptyUserId_ShouldReturnFailure400()
        {
            var handler = CreateHandler();
            var result = await handler.Handle(new UpdateAimLevelCommand { UserId = "" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Account - Update Aim", new TestCaseDetail
            {
                FunctionGroup = "UpdateAimLevelCommandHandler",
                TestCaseID = "UpdateAimLevelCommandHandler_02",
                Description = "Empty string UserId returns 400",
                ExpectedResult = "400 UserId is required.",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserId is empty string" }
            });
        }

        // UpdateAimLevelCommandHandler_03 | A | User not found -> 404
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturn404()
        {
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync((Account?)null);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new UpdateAimLevelCommand { UserId = "u1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Be("User not found.");

            QACollector.LogTestCase("Account - Update Aim", new TestCaseDetail
            {
                FunctionGroup = "UpdateAimLevelCommandHandler",
                TestCaseID = "UpdateAimLevelCommandHandler_03",
                Description = "Non-existent user returns 404",
                ExpectedResult = "404 User not found",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync -> null" }
            });
        }

        // UpdateAimLevelCommandHandler_04 | N | Happy Path -> Updates Level & 200
        [Fact]
        public async Task Handle_HappyPath_ShouldUpdateAimLevelAndReturn200()
        {
            var user = new Account { UserId = "u1", AimLevel = Tokki.Domain.Enums.TopicLevel.Level1 };
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new UpdateAimLevelCommand { UserId = "u1", AimLevel = (Tokki.Domain.Enums.TopicLevel)5 }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            user.AimLevel.Should().Be((Tokki.Domain.Enums.TopicLevel)5);
            user.UpdatedAt.Should().BeAfter(DateTime.MinValue);

            QACollector.LogTestCase("Account - Update Aim", new TestCaseDetail
            {
                FunctionGroup = "UpdateAimLevelCommandHandler",
                TestCaseID = "UpdateAimLevelCommandHandler_04",
                Description = "Valid request mutates AimLevel to requested value",
                ExpectedResult = "200 Success + AimLevel modified",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid user & AimLevel=5" }
            });
        }

        // UpdateAimLevelCommandHandler_05 | B | Test Repo UpdateUserAsync
        [Fact]
        public async Task Handle_Success_ShouldCallUpdateUserAsync()
        {
            var user = new Account { UserId = "u1" };
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            await handler.Handle(new UpdateAimLevelCommand { UserId = "u1", AimLevel = (Tokki.Domain.Enums.TopicLevel)5 }, CancellationToken.None);

            _accountRepoMock.Verify(x => x.UpdateUserAsync(It.Is<Account>(a => a.AimLevel == (Tokki.Domain.Enums.TopicLevel)5)), Times.Once);

            QACollector.LogTestCase("Account - Update Aim", new TestCaseDetail
            {
                FunctionGroup = "UpdateAimLevelCommandHandler",
                TestCaseID = "UpdateAimLevelCommandHandler_05",
                Description = "Verifies UpdateUserAsync is invoked with mutated user",
                ExpectedResult = "Times.Once",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Verify Flow" }
            });
        }

        // UpdateAimLevelCommandHandler_06 | B | Test Repo SaveChangesAsync
        [Fact]
        public async Task Handle_Success_ShouldCallSaveChangesAsync()
        {
            var user = new Account { UserId = "u1" };
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            await handler.Handle(new UpdateAimLevelCommand { UserId = "u1", AimLevel = (Tokki.Domain.Enums.TopicLevel)5 }, CancellationToken.None);

            _accountRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Account - Update Aim", new TestCaseDetail
            {
                FunctionGroup = "UpdateAimLevelCommandHandler",
                TestCaseID = "UpdateAimLevelCommandHandler_06",
                Description = "Verifies SaveChangesAsync is invoked after update",
                ExpectedResult = "Times.Once",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Verify Flow" }
            });
        }
    }
}
