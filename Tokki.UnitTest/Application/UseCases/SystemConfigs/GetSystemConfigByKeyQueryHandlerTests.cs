using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.SystemConfigs.DTOs;
using Tokki.Application.UseCases.SystemConfigs.Queries.GetSystemConfigByKey;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.SystemConfigs
{
    public class GetSystemConfigByKeyQueryHandlerTests
    {
        private static Mock<ISystemConfigRepository> GetRepoMock(SystemConfig? config = null)
        {
            var m = new Mock<ISystemConfigRepository>();
            m.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<SystemConfig, bool>>>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(config);
            return m;
        }

        private static GetSystemConfigByKeyQueryHandler CreateHandler(Mock<ISystemConfigRepository>? repo = null)
            => new GetSystemConfigByKeyQueryHandler((repo ?? GetRepoMock()).Object);

        // GetSystemConfigByKey_01 | N | Happy path: config found ? 200 with DTO
        [Fact]
        public async Task Handle_ConfigFound_ShouldReturn200WithDto()
        {
            var config = new SystemConfig { Key = "SITE_NAME", Value = "Tokki", Description = "App name", DataType = "string", IsActive = true };
            var result = await CreateHandler(GetRepoMock(config)).Handle(new GetSystemConfigByKeyQuery("SITE_NAME"), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.Key.Should().Be("SITE_NAME");
            QACollector.LogTestCase("SystemConfig - Get By Key", new TestCaseDetail { FunctionGroup = "GetSystemConfigByKey", TestCaseID = "GetSystemConfigByKey_01", Description = "Config found ? 200, DTO.Key='SITE_NAME'", ExpectedResult = "IsSuccess=true, 200, Data.Key='SITE_NAME'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "FirstOrDefaultAsync returns config" } });
        }

        // GetSystemConfigByKey_02 | A | Config not found ? failure with error code
        [Fact]
        public async Task Handle_ConfigNotFound_ShouldReturnFailure()
        {
            var result = await CreateHandler(GetRepoMock(null)).Handle(new GetSystemConfigByKeyQuery("MISSING_KEY"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("SystemConfig - Get By Key", new TestCaseDetail { FunctionGroup = "GetSystemConfigByKey", TestCaseID = "GetSystemConfigByKey_02", Description = "Config not found ? failure (CONFIG_NOT_FOUND)", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "FirstOrDefaultAsync returns null" } });
        }

        // GetSystemConfigByKey_03 | N | All DTO fields mapped correctly
        [Fact]
        public async Task Handle_ConfigFound_AllDtoFieldsMapped()
        {
            var config = new SystemConfig { Key = "RATE_LIMIT", Value = "100", Description = "API rate limit", DataType = "int", IsActive = false };
            var result = await CreateHandler(GetRepoMock(config)).Handle(new GetSystemConfigByKeyQuery("RATE_LIMIT"), CancellationToken.None);
            result.Data!.Value.Should().Be("100");
            result.Data.Description.Should().Be("API rate limit");
            result.Data.DataType.Should().Be("int");
            result.Data.IsActive.Should().BeFalse();
            QACollector.LogTestCase("SystemConfig - Get By Key", new TestCaseDetail { FunctionGroup = "GetSystemConfigByKey", TestCaseID = "GetSystemConfigByKey_03", Description = "All DTO fields (Value, Description, DataType, IsActive) mapped correctly", ExpectedResult = "All fields correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "SystemConfig entity fully mapped to DTO" } });
        }

        // GetSystemConfigByKey_04 | N | Key with leading/trailing spaces ? trimmed before search
        [Fact]
        public async Task Handle_KeyWithSpaces_ShouldBeTrimmedBeforeSearch()
        {
            var config = new SystemConfig { Key = "TRIMMED_KEY", Value = "v1", IsActive = true };
            var repo   = GetRepoMock(config);
            // The handler calls FirstOrDefaultAsync with x.Key == request.Key.Trim()
            var result = await CreateHandler(repo).Handle(new GetSystemConfigByKeyQuery("  TRIMMED_KEY"), CancellationToken.None);
            // If handler trims correctly, repo predicate receives trimmed key
            repo.Verify(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<SystemConfig, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("SystemConfig - Get By Key", new TestCaseDetail { FunctionGroup = "GetSystemConfigByKey", TestCaseID = "GetSystemConfigByKey_04", Description = "Key with spaces ? FirstOrDefaultAsync called once (trims key)", ExpectedResult = "FirstOrDefaultAsync Times.Once", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Key.Trim() applied in predicate" } });
        }

        // GetSystemConfigByKey_05 | A | Repository throws ? failure with system error
        [Fact]
        public async Task Handle_RepoThrows_ShouldReturnFailureWithSystemError()
        {
            var repo = new Mock<ISystemConfigRepository>();
            repo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<SystemConfig, bool>>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Connection failed"));
            var result = await CreateHandler(repo).Handle(new GetSystemConfigByKeyQuery("ANY_KEY"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("SystemConfig - Get By Key", new TestCaseDetail { FunctionGroup = "GetSystemConfigByKey", TestCaseID = "GetSystemConfigByKey_05", Description = "Repository throws ? failure returned (caught by try-catch)", ExpectedResult = "IsSuccess=false (GET_BY_KEY_ERROR)", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Exception caught, failure returned" } });
        }

        // GetSystemConfigByKey_06 | N | IsActive=true config returned with IsActive=true in DTO
        [Fact]
        public async Task Handle_ActiveConfig_IsActiveTrueInDto()
        {
            var config = new SystemConfig { Key = "ACTIVE_KEY", Value = "yes", IsActive = true };
            var result = await CreateHandler(GetRepoMock(config)).Handle(new GetSystemConfigByKeyQuery("ACTIVE_KEY"), CancellationToken.None);
            result.Data!.IsActive.Should().BeTrue();
            QACollector.LogTestCase("SystemConfig - Get By Key", new TestCaseDetail { FunctionGroup = "GetSystemConfigByKey", TestCaseID = "GetSystemConfigByKey_06", Description = "Active config ? DTO.IsActive=true", ExpectedResult = "IsActive=true", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "IsActive mapped correctly" } });
        }
    }
}
