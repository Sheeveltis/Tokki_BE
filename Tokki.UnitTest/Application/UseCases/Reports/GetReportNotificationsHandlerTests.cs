using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Reports.Queries.GetReportNotifications;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Reports
{
    public class GetReportNotificationsHandlerTests
    {
        private static GetReportNotificationsHandler CreateHandler(Mock<IReportRepository>? repo = null)
            => new GetReportNotificationsHandler((repo ?? MockReportRepository.GetMock()).Object);

        // GetReportNotifications_01 | N | No unread resolved reports → empty list
        [Fact]
        public async Task Handle_NoUnreadReports_ShouldReturnEmptyList()
        {
            var repo   = MockReportRepository.GetMock(returnedUnread: new List<Report>());
            var result = await CreateHandler(repo).Handle(new GetReportNotificationsQuery { UserId = "USER-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
            QACollector.LogTestCase("Report - Get Notifications", new TestCaseDetail { FunctionGroup = "GetReportNotifications", TestCaseID = "GetReportNotifications_01", Description = "No unread resolved reports → empty success list", ExpectedResult = "IsSuccess=true, Count=0", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetUnreadResolvedReportsAsync returns []" } });
        }

        // GetReportNotifications_02 | N | 2 unread resolved reports → 2 DTOs
        [Fact]
        public async Task Handle_TwoUnreadReports_ShouldReturnTwoDtos()
        {
            var reports = new List<Report>
            {
                MockReportRepository.GetSampleFixedReport("RPT-001", "USER-001"),
                MockReportRepository.GetSampleRejectedReport("RPT-002", "USER-001")
            };
            var repo   = MockReportRepository.GetMock(returnedUnread: reports);
            var result = await CreateHandler(repo).Handle(new GetReportNotificationsQuery { UserId = "USER-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            QACollector.LogTestCase("Report - Get Notifications", new TestCaseDetail { FunctionGroup = "GetReportNotifications", TestCaseID = "GetReportNotifications_02", Description = "2 unread reports → 2 DTOs", ExpectedResult = "IsSuccess=true, Count=2", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "2 unread reports" } });
        }

        // GetReportNotifications_03 | N | DTO fields mapped correctly from entity
        [Fact]
        public async Task Handle_ValidReport_ShouldMapFieldsCorrectly()
        {
            var report = MockReportRepository.GetSampleFixedReport("RPT-MAP-01", "USER-001");
            report.AdminReply = "Fixed in patch";
            report.TargetUrl  = "/vocab/1";
            var repo   = MockReportRepository.GetMock(returnedUnread: new List<Report> { report });
            var result = await CreateHandler(repo).Handle(new GetReportNotificationsQuery { UserId = "USER-001" }, CancellationToken.None);
            var dto = result.Data![0];
            dto.ReportId.Should().Be("RPT-MAP-01");
            dto.Status.Should().Be((int)ReportStatus.Fixed);
            dto.AdminReply.Should().Be("Fixed in patch");
            dto.TargetUrl.Should().Be("/vocab/1");
            QACollector.LogTestCase("Report - Get Notifications", new TestCaseDetail { FunctionGroup = "GetReportNotifications", TestCaseID = "GetReportNotifications_03", Description = "DTO fields mapped: ReportId, Status int, AdminReply, TargetUrl", ExpectedResult = "All fields match", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "All entity fields verified in DTO" } });
        }

        // GetReportNotifications_04 | B | GetUnreadResolvedReportsAsync called with correct UserId
        [Fact]
        public async Task Handle_ValidQuery_GetUnreadCalledWithCorrectUserId()
        {
            var repo    = MockReportRepository.GetMock(returnedUnread: new List<Report>());
            var handler = CreateHandler(repo);
            await handler.Handle(new GetReportNotificationsQuery { UserId = "USER-XYZ" }, CancellationToken.None);
            repo.Verify(x => x.GetUnreadResolvedReportsAsync("USER-XYZ"), Times.Once);
            QACollector.LogTestCase("Report - Get Notifications", new TestCaseDetail { FunctionGroup = "GetReportNotifications", TestCaseID = "GetReportNotifications_04", Description = "GetUnreadResolvedReportsAsync called with exact UserId", ExpectedResult = "GetUnreadResolvedReportsAsync('USER-XYZ') Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "UserId='USER-XYZ' in query" } });
        }

        // GetReportNotifications_05 | A | Repository throws → failure returned (try/catch)
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturnFailure()
        {
            var repo = new Mock<IReportRepository>();
            repo.Setup(x => x.GetUnreadResolvedReportsAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("DB error"));
            var result = await CreateHandler(repo).Handle(new GetReportNotificationsQuery { UserId = "USER-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("Report - Get Notifications", new TestCaseDetail { FunctionGroup = "GetReportNotifications", TestCaseID = "GetReportNotifications_05", Description = "Repository throws → caught → ReportFetchFailed failure", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetUnreadResolvedReportsAsync throws", "catch block returns failure" } });
        }

        // GetReportNotifications_06 | N | Rejected report Status mapped as int=3
        [Fact]
        public async Task Handle_RejectedReport_StatusMappedAsInt3()
        {
            var report = MockReportRepository.GetSampleRejectedReport("RPT-003", "USER-001");
            var repo   = MockReportRepository.GetMock(returnedUnread: new List<Report> { report });
            var result = await CreateHandler(repo).Handle(new GetReportNotificationsQuery { UserId = "USER-001" }, CancellationToken.None);
            result.Data![0].Status.Should().Be(3); // Rejected = 3
            QACollector.LogTestCase("Report - Get Notifications", new TestCaseDetail { FunctionGroup = "GetReportNotifications", TestCaseID = "GetReportNotifications_06", Description = "ReportStatus.Rejected → DTO.Status=3", ExpectedResult = "DTO.Status=3", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Status=Rejected(3)" } });
        }
    }
}
