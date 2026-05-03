using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.SystemConfigs.DTOs;
using Tokki.Application.UseCases.SystemConfigs.Queries.GetAll;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.SystemConfigs
{
    public class GetAllSystemConfigsQueryHandlerTests
    {
        private static Mock<ISystemConfigRepository> GetRepoMock(
            List<SystemConfig>? items = null, int total = 0)
        {
            var m = new Mock<ISystemConfigRepository>();
            m.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SystemConfigType?>()))
             .ReturnsAsync((items ?? new List<SystemConfig>(), total));
            return m;
        }

        private static GetAllSystemConfigsQueryHandler CreateHandler(Mock<ISystemConfigRepository>? repo = null)
            => new GetAllSystemConfigsQueryHandler((repo ?? GetRepoMock()).Object);

        private static GetAllSystemConfigsQuery MakeQuery(int page = 1, int size = 10)
            => new GetAllSystemConfigsQuery { PageNumber = page, PageSize = size };

        private static List<SystemConfig> SampleConfigs() => new List<SystemConfig>
        {
            new SystemConfig { Key = "MAX_ATTEMPTS", Value = "3",    Description = "Max login attempts", DataType = "int",    IsActive = true  },
            new SystemConfig { Key = "SITE_NAME",    Value = "Tokki", Description = "App name",           DataType = "string", IsActive = true  },
            new SystemConfig { Key = "MAINTENANCE",  Value = "false", Description = "Maintenance mode",   DataType = "bool",   IsActive = false }
        };

        // GetAllSystemConfigs_01 | N | Happy path: 3 configs → PagedResult Count=3
        [Fact]
        public async Task Handle_RepoReturnsConfigs_ShouldReturn200WithPagedResult()
        {
            var repo   = GetRepoMock(SampleConfigs(), total: 3);
            var result = await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.Items.Should().HaveCount(3);
            result.Data.TotalCount.Should().Be(3);
            QACollector.LogTestCase("SystemConfig - Get All", new TestCaseDetail { FunctionGroup = "GetAllSystemConfigs", TestCaseID = "GetAllSystemConfigs_01", Description = "3 configs → PagedResult Count=3, TotalCount=3", ExpectedResult = "IsSuccess=true, Items.Count=3, TotalCount=3", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetPagedAsync returns 3 items" } });
        }

        // GetAllSystemConfigs_02 | N | Config fields mapped to DTO correctly
        [Fact]
        public async Task Handle_RepoReturnsConfigs_DtoFieldsMappedCorrectly()
        {
            var configs = new List<SystemConfig> { new SystemConfig { Key = "TEST_KEY", Value = "test_val", Description = "desc", DataType = "string", IsActive = true } };
            var repo    = GetRepoMock(configs, total: 1);
            var result  = await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            result.Data!.Items[0].Key.Should().Be("TEST_KEY");
            result.Data.Items[0].Value.Should().Be("test_val");
            result.Data.Items[0].IsActive.Should().BeTrue();
            QACollector.LogTestCase("SystemConfig - Get All", new TestCaseDetail { FunctionGroup = "GetAllSystemConfigs", TestCaseID = "GetAllSystemConfigs_02", Description = "SystemConfig entity mapped to SystemConfigDto correctly", ExpectedResult = "Key='TEST_KEY', Value='test_val', IsActive=true", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Entity fields mapped to DTO" } });
        }

        // GetAllSystemConfigs_03 | B | GetPagedAsync called with correct page params
        [Fact]
        public async Task Handle_WithPaging_GetPagedCalledWithCorrectParams()
        {
            var repo = GetRepoMock();
            await CreateHandler(repo).Handle(MakeQuery(page: 2, size: 5), CancellationToken.None);
            repo.Verify(x => x.GetPagedAsync(2, 5, It.IsAny<SystemConfigType?>()), Times.Once);
            QACollector.LogTestCase("SystemConfig - Get All", new TestCaseDetail { FunctionGroup = "GetAllSystemConfigs", TestCaseID = "GetAllSystemConfigs_03", Description = "GetPagedAsync called with PageNumber=2, PageSize=5", ExpectedResult = "Times.Once with params (2,5)", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Paging params forwarded correctly" } });
        }

        // GetAllSystemConfigs_04 | N | Empty list → 200 with empty PagedResult
        [Fact]
        public async Task Handle_NoConfigs_ShouldReturn200WithEmptyPage()
        {
            var result = await CreateHandler(GetRepoMock(new List<SystemConfig>(), 0)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
            QACollector.LogTestCase("SystemConfig - Get All", new TestCaseDetail { FunctionGroup = "GetAllSystemConfigs", TestCaseID = "GetAllSystemConfigs_04", Description = "No configs → 200 with empty paged result", ExpectedResult = "IsSuccess=true, Items=[], TotalCount=0", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No system configs exist" } });
        }

        // GetAllSystemConfigs_05 | N | Paged metadata (PageNumber, PageSize) correct
        [Fact]
        public async Task Handle_WithPage3Size20_PagingMetadataCorrect()
        {
            var configs = new List<SystemConfig> { new SystemConfig { Key = "K1" } };
            var result  = await CreateHandler(GetRepoMock(configs, total: 100)).Handle(MakeQuery(page: 3, size: 20), CancellationToken.None);
            result.Data!.PageNumber.Should().Be(3);
            result.Data.PageSize.Should().Be(20);
            result.Data.TotalCount.Should().Be(100);
            QACollector.LogTestCase("SystemConfig - Get All", new TestCaseDetail { FunctionGroup = "GetAllSystemConfigs", TestCaseID = "GetAllSystemConfigs_05", Description = "Paging metadata: Page=3, Size=20, TotalCount=100", ExpectedResult = "PageNumber=3, PageSize=20, TotalCount=100", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TotalCount=100, page 3 of 20" } });
        }

        // GetAllSystemConfigs_06 | N | IsActive=false configs included in result
        [Fact]
        public async Task Handle_ConfigsIncludeInactive_AllReturnedInList()
        {
            var configs = new List<SystemConfig>
            {
                new SystemConfig { Key = "ACTIVE", IsActive = true  },
                new SystemConfig { Key = "INACTIVE", IsActive = false }
            };
            var result = await CreateHandler(GetRepoMock(configs, total: 2)).Handle(MakeQuery(), CancellationToken.None);
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items.Any(x => !x.IsActive).Should().BeTrue();
            QACollector.LogTestCase("SystemConfig - Get All", new TestCaseDetail { FunctionGroup = "GetAllSystemConfigs", TestCaseID = "GetAllSystemConfigs_06", Description = "Inactive configs included in results (no filter by IsActive)", ExpectedResult = "Items.Count=2 (including inactive)", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Both active and inactive returned" } });
        }
    }
}
