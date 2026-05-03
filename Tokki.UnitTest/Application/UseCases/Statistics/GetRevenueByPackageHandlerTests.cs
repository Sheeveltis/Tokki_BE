using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Payments.DTOs;
using Tokki.Application.UseCases.Statistics.Queries;
using Tokki.UnitTests.UseCases.Statistics.Queries.GetRevenueByPackage;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Statistics
{
    public class GetRevenueByPackageHandlerTests
    {
        private static Mock<IStatisticsRepository> GetRepoMock(List<RevenueByPackageDto>? data = null)
        {
            var m = new Mock<IStatisticsRepository>();
            m.Setup(x => x.GetRevenueByPackageAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
             .ReturnsAsync(data ?? new List<RevenueByPackageDto>());
            return m;
        }

        private static GetRevenueByPackageHandler CreateHandler(Mock<IStatisticsRepository>? repo = null)
            => new GetRevenueByPackageHandler((repo ?? GetRepoMock()).Object);

        private static GetRevenueByPackageQuery MakeQuery(DateTime? start = null, DateTime? end = null)
            => new GetRevenueByPackageQuery { StartDate = start ?? new DateTime(2024, 1, 1), EndDate = end ?? new DateTime(2024, 12, 31) };

        private static List<RevenueByPackageDto> SampleData() => new List<RevenueByPackageDto>
        {
            new RevenueByPackageDto { PackageName = "Premium 1M", DurationDays = 30, Revenue = 2000m, SalesCount = 10, Percentage = 40.0 },
            new RevenueByPackageDto { PackageName = "Premium 3M", DurationDays = 90, Revenue = 3000m, SalesCount = 5, Percentage = 60.0 }
        };

        // GetRevenueByPackage_01 | N | Happy path: 2 packages returned → 200
        [Fact]
        public async Task Handle_RepoReturnsData_ShouldReturn200With2Items()
        {
            var result = await CreateHandler(GetRepoMock(SampleData())).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            QACollector.LogTestCase("Statistics - Revenue By Package", new TestCaseDetail { FunctionGroup = "GetRevenueByPackage", TestCaseID = "GetRevenueByPackage_01", Description = "2 packages returned → 200, Count=2", ExpectedResult = "IsSuccess=true, Data.Count=2", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetRevenueByPackageAsync returns 2 items" } });
        }

        // GetRevenueByPackage_02 | N | Package fields mapped correctly
        [Fact]
        public async Task Handle_RepoReturnsData_FieldsMappedCorrectly()
        {
            var data   = new List<RevenueByPackageDto> { new RevenueByPackageDto { PackageName = "Gold", Revenue = 9999m, SalesCount = 3, Percentage = 100.0 } };
            var result = await CreateHandler(GetRepoMock(data)).Handle(MakeQuery(), CancellationToken.None);
            result.Data![0].PackageName.Should().Be("Gold");
            result.Data[0].Revenue.Should().Be(9999m);
            result.Data[0].SalesCount.Should().Be(3);
            QACollector.LogTestCase("Statistics - Revenue By Package", new TestCaseDetail { FunctionGroup = "GetRevenueByPackage", TestCaseID = "GetRevenueByPackage_02", Description = "Package fields (PackageName, Revenue, SalesCount) mapped correctly", ExpectedResult = "All fields verified", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "DTO fields passed through" } });
        }

        // GetRevenueByPackage_03 | B | GetRevenueByPackageAsync called with correct date range
        [Fact]
        public async Task Handle_WithDateRange_RepoCalledWithCorrectDates()
        {
            var start = new DateTime(2024, 6, 1);
            var end   = new DateTime(2024, 6, 30);
            var repo  = GetRepoMock();
            await CreateHandler(repo).Handle(MakeQuery(start, end), CancellationToken.None);
            repo.Verify(x => x.GetRevenueByPackageAsync(start, end), Times.Once);
            QACollector.LogTestCase("Statistics - Revenue By Package", new TestCaseDetail { FunctionGroup = "GetRevenueByPackage", TestCaseID = "GetRevenueByPackage_03", Description = "GetRevenueByPackageAsync called with correct StartDate/EndDate", ExpectedResult = "Times.Once with correct dates", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Dates forwarded correctly to repo" } });
        }

        // GetRevenueByPackage_04 | N | Empty list → 200 with empty data
        [Fact]
        public async Task Handle_NoData_ShouldReturn200WithEmptyList()
        {
            var result = await CreateHandler(GetRepoMock(new List<RevenueByPackageDto>())).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
            QACollector.LogTestCase("Statistics - Revenue By Package", new TestCaseDetail { FunctionGroup = "GetRevenueByPackage", TestCaseID = "GetRevenueByPackage_04", Description = "No data → 200 with empty list", ExpectedResult = "IsSuccess=true, Data=[]", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No revenue records in range" } });
        }

        // GetRevenueByPackage_05 | N | Percentage field correctly included in result
        [Fact]
        public async Task Handle_ReturnsData_PercentageIsCorrect()
        {
            var data   = new List<RevenueByPackageDto> { new RevenueByPackageDto { PackageName = "Silver", Percentage = 75.5 } };
            var result = await CreateHandler(GetRepoMock(data)).Handle(MakeQuery(), CancellationToken.None);
            result.Data![0].Percentage.Should().Be(75.5);
            QACollector.LogTestCase("Statistics - Revenue By Package", new TestCaseDetail { FunctionGroup = "GetRevenueByPackage", TestCaseID = "GetRevenueByPackage_05", Description = "Percentage=75.5 passed through correctly", ExpectedResult = "Percentage=75.5", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Percentage field present" } });
        }

        // GetRevenueByPackage_06 | A | Repository throws → exception propagates
        [Fact]
        public async Task Handle_RepoThrows_ShouldPropagateException()
        {
            var repo = new Mock<IStatisticsRepository>();
            repo.Setup(x => x.GetRevenueByPackageAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("DB error"));
            var act = async () => await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
            QACollector.LogTestCase("Statistics - Revenue By Package", new TestCaseDetail { FunctionGroup = "GetRevenueByPackage", TestCaseID = "GetRevenueByPackage_06", Description = "Repository throws → exception propagates", ExpectedResult = "Exception thrown", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetRevenueByPackageAsync throws" } });
        }
    }
}
