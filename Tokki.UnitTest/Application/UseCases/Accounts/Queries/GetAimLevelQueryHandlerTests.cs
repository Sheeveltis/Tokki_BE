using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Queries.GetAimLevel;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts.Queries
{
    public class GetAimLevelQueryHandlerTests
    {
        private readonly Mock<IAccountRepository> _accountRepoMock = new();

        private GetAimLevelQueryHandler CreateHandler()
        {
            return new GetAimLevelQueryHandler(_accountRepoMock.Object);
        }

        // TC-ACC-GAL-01 | A | User Not Found -> 404
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturn404()
        {
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync((Account?)null);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetAimLevelQuery { UserId = "u1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Be("Người dùng không tồn tại.");

            QACollector.LogTestCase("Account - Get Aim Level", new TestCaseDetail
            {
                FunctionGroup = "GetAimLevelQueryHandler",
                TestCaseID = "TC-ACC-GAL-01",
                Description = "User not found returns 404",
                ExpectedResult = "404 Null user",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync -> null" }
            });
        }

        // TC-ACC-GAL-02 | N | Happy Path -> Null Aim Level -> 200
        [Fact]
        public async Task Handle_NullAimLevel_ShouldReturn200WithNullData()
        {
            var user = new Account { UserId = "u1", AimLevel = null };
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetAimLevelQuery { UserId = "u1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeNull();

            QACollector.LogTestCase("Account - Get Aim Level", new TestCaseDetail
            {
                FunctionGroup = "GetAimLevelQueryHandler",
                TestCaseID = "TC-ACC-GAL-02",
                Description = "Returns 200 even if AimLevel is null",
                ExpectedResult = "200 Success + null",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "User.AimLevel == null" }
            });
        }

        // TC-ACC-GAL-03 | N | Happy Path -> Aim Level 1 -> 200
        [Fact]
        public async Task Handle_Level1_ShouldReturn200WithCorrectData()
        {
            var user = new Account { UserId = "u1", AimLevel = Tokki.Domain.Enums.TopicLevel.Level1 }; // Typically 1 = Level1
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetAimLevelQuery { UserId = "u1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be((TopicLevel)1);

            QACollector.LogTestCase("Account - Get Aim Level", new TestCaseDetail
            {
                FunctionGroup = "GetAimLevelQueryHandler",
                TestCaseID = "TC-ACC-GAL-03",
                Description = "Returns 200 with AimLevel 1 casted to TopicLevel",
                ExpectedResult = "200 Success + Level 1",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "User.AimLevel == 1" }
            });
        }

        // TC-ACC-GAL-04 | N | Happy Path -> Aim Level 6 -> 200
        [Fact]
        public async Task Handle_Level6_ShouldReturn200WithCorrectData()
        {
            var user = new Account { UserId = "u1", AimLevel = Tokki.Domain.Enums.TopicLevel.Level6 }; // Max level limit check
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetAimLevelQuery { UserId = "u1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be((TopicLevel)6);

            QACollector.LogTestCase("Account - Get Aim Level", new TestCaseDetail
            {
                FunctionGroup = "GetAimLevelQueryHandler",
                TestCaseID = "TC-ACC-GAL-04",
                Description = "Returns 200 with AimLevel 6 casted to TopicLevel",
                ExpectedResult = "200 Success + Level 6",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "User.AimLevel == 6" }
            });
        }

        // TC-ACC-GAL-05 | B | Exact UserId passed to GetByIdAsync
        [Fact]
        public async Task Handle_ShouldCallGetByIdAsyncWithUserId()
        {
            var user = new Account { UserId = "test-id", AimLevel = Tokki.Domain.Enums.TopicLevel.Level2 };
            _accountRepoMock.Setup(x => x.GetByIdAsync("test-id")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            await handler.Handle(new GetAimLevelQuery { UserId = "test-id" }, CancellationToken.None);

            _accountRepoMock.Verify(x => x.GetByIdAsync("test-id"), Times.Once);

            QACollector.LogTestCase("Account - Get Aim Level", new TestCaseDetail
            {
                FunctionGroup = "GetAimLevelQueryHandler",
                TestCaseID = "TC-ACC-GAL-05",
                Description = "Verifies Repository is injected with precise User ID",
                ExpectedResult = "Verify Times.Once",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserId matches exactly" }
            });
        }

        // TC-ACC-GAL-06 | B | Correct Success Message
        [Fact]
        public async Task Handle_Success_ShouldReturnExpectedMessage()
        {
            var user = new Account { UserId = "u1", AimLevel = null };
            _accountRepoMock.Setup(x => x.GetByIdAsync("u1")).ReturnsAsync(user);
            
            var handler = CreateHandler();
            var result = await handler.Handle(new GetAimLevelQuery { UserId = "u1" }, CancellationToken.None);

            result.Message.Should().Be("Lấy Aim Level thành công.");

            QACollector.LogTestCase("Account - Get Aim Level", new TestCaseDetail
            {
                FunctionGroup = "GetAimLevelQueryHandler",
                TestCaseID = "TC-ACC-GAL-06",
                Description = "Success branch returns expected message",
                ExpectedResult = "'Lấy Aim Level thành công.'",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Happy Path Message check" }
            });
        }
    }
}
