using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Reports.Queries.GetAllReports;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Reports
{
    public class GetAllReportsHandlerTests
    {
        private static GetAllReportsHandler CreateHandler(Mock<IReportRepository>? repo = null)
            => new GetAllReportsHandler((repo ?? MockReportRepository.GetMock()).Object);

        [Fact]
        public async Task Handle_EmptyRepository_ShouldReturnEmptyList()
        {
            var repo    = MockReportRepository.GetMock(returnedAll: new List<Report>());
            var result  = await CreateHandler(repo).Handle(new GetAllReportsQuery { Status = null }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
            QACollector.LogTestCase("Report - Get All", new TestCaseDetail { FunctionGroup = "GetAllReports", TestCaseID = "GetAllReports_01", Description = "Empty repo → empty DTO list", ExpectedResult = "IsSuccess=true, Count=0", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetAllAsync returns []" } });
        }

        [Fact]
        public async Task Handle_ThreeReports_ShouldReturnThreeDtos()
        {
            var reports = MockReportRepository.GetSampleReportList(3);
            var repo    = MockReportRepository.GetMock(returnedAll: reports);
            var result  = await CreateHandler(repo).Handle(new GetAllReportsQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            QACollector.LogTestCase("Report - Get All", new TestCaseDetail { FunctionGroup = "GetAllReports", TestCaseID = "GetAllReports_02", Description = "3 reports → 3 DTOs", ExpectedResult = "Count=3", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "3 reports in repo" } });
        }

        [Fact]
        public async Task Handle_ValidReport_ShouldMapFieldsCorrectly()
        {
            var report = new Report { Id = "RPT-MAP-01", Status = ReportStatus.Fixed, AdminReply = "Fixed", ResolvedAt = DateTime.UtcNow, Description = "Map test", TargetUrl = "/test", ImageUrl = "http://img/1.png" };
            var repo   = MockReportRepository.GetMock(returnedAll: new List<Report> { report });
            var result = await CreateHandler(repo).Handle(new GetAllReportsQuery(), CancellationToken.None);
            var dto = result.Data![0];
            dto.ReportId.Should().Be("RPT-MAP-01");
            dto.Status.Should().Be((int)ReportStatus.Fixed);
            dto.Description.Should().Be("Map test");
            QACollector.LogTestCase("Report - Get All", new TestCaseDetail { FunctionGroup = "GetAllReports", TestCaseID = "GetAllReports_03", Description = "DTO fields mapped correctly", ExpectedResult = "All fields match", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "All entity fields verified" } });
        }

        [Fact]
        public async Task Handle_WithStatusFilter_GetAllAsyncCalledWithFilter()
        {
            var repo    = MockReportRepository.GetMock(returnedAll: new List<Report>());
            var handler = CreateHandler(repo);
            await handler.Handle(new GetAllReportsQuery { Status = ReportStatus.Pending }, CancellationToken.None);
            repo.Verify(x => x.GetAllAsync(ReportStatus.Pending), Times.Once);
            QACollector.LogTestCase("Report - Get All", new TestCaseDetail { FunctionGroup = "GetAllReports", TestCaseID = "GetAllReports_04", Description = "Status filter passed to GetAllAsync", ExpectedResult = "GetAllAsync(Pending) Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Status=Pending" } });
        }

        [Fact]
        public async Task Handle_NullStatusFilter_GetAllAsyncCalledWithNull()
        {
            var repo    = MockReportRepository.GetMock(returnedAll: new List<Report>());
            var handler = CreateHandler(repo);
            await handler.Handle(new GetAllReportsQuery { Status = null }, CancellationToken.None);
            repo.Verify(x => x.GetAllAsync(null), Times.Once);
            QACollector.LogTestCase("Report - Get All", new TestCaseDetail { FunctionGroup = "GetAllReports", TestCaseID = "GetAllReports_05", Description = "Null status filter → GetAllAsync(null)", ExpectedResult = "GetAllAsync(null) Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Status=null" } });
        }

        [Fact]
        public async Task Handle_FixedReport_StatusMappedAsIntValue2()
        {
            var report = MockReportRepository.GetSampleFixedReport();
            var repo   = MockReportRepository.GetMock(returnedAll: new List<Report> { report });
            var result = await CreateHandler(repo).Handle(new GetAllReportsQuery(), CancellationToken.None);
            result.Data![0].Status.Should().Be(2);
            QACollector.LogTestCase("Report - Get All", new TestCaseDetail { FunctionGroup = "GetAllReports", TestCaseID = "GetAllReports_06", Description = "ReportStatus.Fixed cast to int=2 in DTO", ExpectedResult = "DTO.Status=2", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Status=Fixed(2)" } });
        }
    }
}
