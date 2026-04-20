using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.Queries.GetMyLevel;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts
{
    public class GetMyLevelQueryHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static GetMyLevelQueryHandler CreateHandler(Mock<IAccountRepository>? repo = null)
        {
            return new GetMyLevelQueryHandler((repo ?? MockAccountRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // Get_My_Level_01 | A | User not found → Return 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UserNotFound_ShouldReturn404()
        {
            var result = await CreateHandler().Handle(new GetMyLevelQuery { UserId = "MISSING-USER" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Be("Không tìm thấy người dùng.");
            result.Errors.Should().Contain(AppErrors.UserNotFound);

            QACollector.LogTestCase("Account - Get My Level", new TestCaseDetail
            {
                FunctionGroup     = "Get My Level",
                TestCaseID        = "Get_My_Level_01",
                Description       = "Query with non-existent UserId",
                ExpectedResult    = "Return 404 UserNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_My_Level_02 | N | User exists, Level is Null → Return null Level
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullLevel_ShouldReturnNullInDto()
        {
            var user = MockAccountRepository.GetActiveUser("USER-NL");
            user.Level = null;

            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync(user.UserId)).ReturnsAsync(user);

            var result = await CreateHandler(mockRepo).Handle(new GetMyLevelQuery { UserId = user.UserId }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Level.Should().BeNull();

            QACollector.LogTestCase("Account - Get My Level", new TestCaseDetail
            {
                FunctionGroup     = "Get My Level",
                TestCaseID        = "Get_My_Level_02",
                Description       = "User exists but Level is null",
                ExpectedResult    = "Return 200 with Level = null",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "User.Level is null", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_My_Level_03 | N | User exists, Level is 1 → Return 1
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Level1_ShouldReturn1()
        {
            var user = MockAccountRepository.GetActiveUser("USER-L1");
            user.Level = TopicLevel.Level1;

            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync(user.UserId)).ReturnsAsync(user);

            var result = await CreateHandler(mockRepo).Handle(new GetMyLevelQuery { UserId = user.UserId }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Level.Should().Be(1);

            QACollector.LogTestCase("Account - Get My Level", new TestCaseDetail
            {
                FunctionGroup     = "Get My Level",
                TestCaseID        = "Get_My_Level_03",
                Description       = "User exists and Level is Level1",
                ExpectedResult    = "Return 200 with Level = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "User.Level is Level1", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_My_Level_04 | N | User exists, Level is 6 → Return 6
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Level6_ShouldReturn6()
        {
            var user = MockAccountRepository.GetActiveUser("USER-L6");
            user.Level = TopicLevel.Level6;

            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync(user.UserId)).ReturnsAsync(user);

            var result = await CreateHandler(mockRepo).Handle(new GetMyLevelQuery { UserId = user.UserId }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Level.Should().Be(6);

            QACollector.LogTestCase("Account - Get My Level", new TestCaseDetail
            {
                FunctionGroup     = "Get My Level",
                TestCaseID        = "Get_My_Level_04",
                Description       = "User exists and Level is Level6",
                ExpectedResult    = "Return 200 with Level = 6",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "User.Level is Level6", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_My_Level_05 | N | Verify GetByIdAsync is called exactly once
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldCallRepositoryOnce()
        {
            var user = MockAccountRepository.GetActiveUser("USER-SYNC");
            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync(user.UserId)).ReturnsAsync(user);

            await CreateHandler(mockRepo).Handle(new GetMyLevelQuery { UserId = user.UserId }, CancellationToken.None);

            mockRepo.Verify(x => x.GetByIdAsync(user.UserId), Times.Once);

            QACollector.LogTestCase("Account - Get My Level", new TestCaseDetail
            {
                FunctionGroup     = "Get My Level",
                TestCaseID        = "Get_My_Level_05",
                Description       = "Verify Repository GetByIdAsync is called exactly once",
                ExpectedResult    = "GetByIdAsync called 1 time",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync called x1" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_My_Level_06 | B | Empty UserId edge case
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyUserId_ShouldReturn404()
        {
            var mockRepo = MockAccountRepository.GetMock();
            mockRepo.Setup(x => x.GetByIdAsync("")).ReturnsAsync((Account?)null);

            var result = await CreateHandler(mockRepo).Handle(new GetMyLevelQuery { UserId = "" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Account - Get My Level", new TestCaseDetail
            {
                FunctionGroup     = "Get My Level",
                TestCaseID        = "Get_My_Level_06",
                Description       = "Empty string provided as UserId",
                ExpectedResult    = "Return 404 UserNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty string mapped to null result", "Return 404" }
            });
        }
    }
}
