using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Payments.DTOs;
using Tokki.Application.UseCases.Statistics.Queries;
using Tokki.UnitTests.UseCases.Statistics.Queries.GetTransactionsReport;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Statistics
{
    public class GetTransactionsReportHandlerTests
    {
        private static Mock<IStatisticsRepository> GetRepoMock(
            List<TransactionReportDto>? items = null, int total = 0)
        {
            var m = new Mock<IStatisticsRepository>();
            m.Setup(x => x.GetTransactionsAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                    It.IsAny<int>(), It.IsAny<int>()))
             .ReturnsAsync((items ?? new List<TransactionReportDto>(), total));
            return m;
        }

        private static GetTransactionsReportHandler CreateHandler(Mock<IStatisticsRepository>? repo = null)
            => new GetTransactionsReportHandler((repo ?? GetRepoMock()).Object);

        private static GetTransactionsReportQuery MakeQuery(
            string? search = null, string? status = null, int page = 1, int pageSize = 10)
            => new GetTransactionsReportQuery { Search = search, Status = status, Page = page, PageSize = pageSize };

        private static List<TransactionReportDto> SampleTransactions() => new List<TransactionReportDto>
        {
            new TransactionReportDto { TransactionId = "TX-001", FullName = "Nguyen Van A", Amount = 299m, Status = "Success" },
            new TransactionReportDto { TransactionId = "TX-002", FullName = "Tran Thi B",  Amount = 499m, Status = "Pending" }
        };

        // TC-STAT-TR-01 | N | Happy path: 2 transactions returned → PagedResult count=2
        [Fact]
        public async Task Handle_RepoReturnsData_ShouldReturnPagedResult()
        {
            var repo   = GetRepoMock(SampleTransactions(), total: 2);
            var result = await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
            QACollector.LogTestCase("Statistics - Transactions Report", new TestCaseDetail { FunctionGroup = "GetTransactionsReport", TestCaseID = "TC-STAT-TR-01", Description = "2 transactions → PagedResult Count=2, TotalCount=2", ExpectedResult = "IsSuccess=true, Items.Count=2", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTransactionsAsync returns 2 items" } });
        }

        // TC-STAT-TR-02 | N | Transaction fields (TransactionId, FullName, Amount, Status) mapped correctly
        [Fact]
        public async Task Handle_ReturnsData_TransactionFieldsMappedCorrectly()
        {
            var items  = new List<TransactionReportDto> { new TransactionReportDto { TransactionId = "TX-999", FullName = "Test User", Amount = 1234m, Status = "Success" } };
            var repo   = GetRepoMock(items, total: 1);
            var result = await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            result.Data!.Items[0].TransactionId.Should().Be("TX-999");
            result.Data.Items[0].Amount.Should().Be(1234m);
            result.Data.Items[0].Status.Should().Be("Success");
            QACollector.LogTestCase("Statistics - Transactions Report", new TestCaseDetail { FunctionGroup = "GetTransactionsReport", TestCaseID = "TC-STAT-TR-02", Description = "TX-999 fields mapped: Amount=1234, Status=Success", ExpectedResult = "All fields correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "DTO fields passed through correctly" } });
        }

        // TC-STAT-TR-03 | B | GetTransactionsAsync called with correct filters
        [Fact]
        public async Task Handle_WithFilters_RepoCalledWithCorrectParams()
        {
            var from = new DateTime(2024, 1, 1);
            var to   = new DateTime(2024, 3, 31);
            var repo = GetRepoMock();
            await CreateHandler(repo).Handle(new GetTransactionsReportQuery { Search = "john", Status = "Success", FromDate = from, ToDate = to, Page = 2, PageSize = 5 }, CancellationToken.None);
            repo.Verify(x => x.GetTransactionsAsync("john", "Success", from, to, 2, 5), Times.Once);
            QACollector.LogTestCase("Statistics - Transactions Report", new TestCaseDetail { FunctionGroup = "GetTransactionsReport", TestCaseID = "TC-STAT-TR-03", Description = "GetTransactionsAsync called with all filter params correctly", ExpectedResult = "Times.Once with correct params", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Search, Status, FromDate, ToDate, Page, PageSize forwarded to repo" } });
        }

        // TC-STAT-TR-04 | N | Empty result → 200 with empty PagedResult
        [Fact]
        public async Task Handle_NoTransactions_ShouldReturn200WithEmptyPage()
        {
            var result = await CreateHandler(GetRepoMock(new List<TransactionReportDto>(), 0)).Handle(MakeQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
            QACollector.LogTestCase("Statistics - Transactions Report", new TestCaseDetail { FunctionGroup = "GetTransactionsReport", TestCaseID = "TC-STAT-TR-04", Description = "No transactions → 200 with empty page", ExpectedResult = "IsSuccess=true, Items=[], TotalCount=0", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "No records in filter range" } });
        }

        // TC-STAT-TR-05 | N | PagedResult pagination metadata is correct
        [Fact]
        public async Task Handle_WithPaging_PagedMetadataIsCorrect()
        {
            var items  = new List<TransactionReportDto> { new TransactionReportDto { TransactionId = "TX-001" } };
            var result = await CreateHandler(GetRepoMock(items, total: 50)).Handle(MakeQuery(page: 3, pageSize: 10), CancellationToken.None);
            result.Data!.TotalCount.Should().Be(50);
            result.Data.PageNumber.Should().Be(3);
            result.Data.PageSize.Should().Be(10);
            QACollector.LogTestCase("Statistics - Transactions Report", new TestCaseDetail { FunctionGroup = "GetTransactionsReport", TestCaseID = "TC-STAT-TR-05", Description = "Paging: TotalCount=50, Page=3, PageSize=10 correct", ExpectedResult = "Paging metadata correct", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TotalCount=50, page 3 of 10" } });
        }

        // TC-STAT-TR-06 | A | Repository throws → exception propagates
        [Fact]
        public async Task Handle_RepoThrows_ShouldPropagateException()
        {
            var repo = new Mock<IStatisticsRepository>();
            repo.Setup(x => x.GetTransactionsAsync(It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("DB error"));
            var act = async () => await CreateHandler(repo).Handle(MakeQuery(), CancellationToken.None);
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
            QACollector.LogTestCase("Statistics - Transactions Report", new TestCaseDetail { FunctionGroup = "GetTransactionsReport", TestCaseID = "TC-STAT-TR-06", Description = "Repository throws → exception propagates", ExpectedResult = "Exception thrown", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetTransactionsAsync throws" } });
        }
    }
}
