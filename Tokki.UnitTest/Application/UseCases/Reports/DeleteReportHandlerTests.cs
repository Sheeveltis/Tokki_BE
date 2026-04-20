using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Reports.Commands.DeleteReport;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Reports
{
    public class DeleteReportHandlerTests
    {
        private static DeleteReportHandler CreateHandler(Mock<IReportRepository>? repo = null)
            => new DeleteReportHandler((repo ?? MockReportRepository.GetMock()).Object);

        // ═══════════════════════════════════════════════════════════
        // DeleteReport_01 | A | Report not found → failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ReportNotFound_ShouldReturnFailure()
        {
            // Arrange
            var repo    = MockReportRepository.GetMock(returnedById: null);
            var handler = CreateHandler(repo);
            var command = new DeleteReportCommand { ReportId = "RPT-MISSING", UserId = "USER-001" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Report - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteReport",
                TestCaseID        = "DeleteReport_01",
                Description       = "Report not found → ReportNotFound failure",
                ExpectedResult    = "IsSuccess=false",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "failure" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeleteReport_02 | A | Non-admin deleting other user's report → unauthorized
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NonAdminWrongUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var report  = MockReportRepository.GetSamplePendingReport(userId: "OWNER-001");
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new DeleteReportCommand
            {
                ReportId = report.Id,
                UserId   = "OTHER-USER", // not the owner
                IsAdmin  = false
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            repo.Verify(x => x.DeleteAsync(It.IsAny<Report>()), Times.Never);

            QACollector.LogTestCase("Report - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteReport",
                TestCaseID        = "DeleteReport_02",
                Description       = "Non-admin user tries to delete another user's report → ReportUnauthorized failure",
                ExpectedResult    = "IsSuccess=false, DeleteAsync never called",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsAdmin=false", "UserId != report.UserId", "unauthorized" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeleteReport_03 | A | Non-admin deleting non-Pending report → cannot delete
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NonAdminNonPendingReport_ShouldReturnCannotDelete()
        {
            // Arrange
            var report  = MockReportRepository.GetSampleFixedReport(userId: "USER-001");
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new DeleteReportCommand
            {
                ReportId = report.Id,
                UserId   = "USER-001", // owner, but report is Fixed
                IsAdmin  = false
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            repo.Verify(x => x.DeleteAsync(It.IsAny<Report>()), Times.Never);

            QACollector.LogTestCase("Report - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteReport",
                TestCaseID        = "DeleteReport_03",
                Description       = "Non-admin deletes own report with status Fixed → ReportCannotDelete failure",
                ExpectedResult    = "IsSuccess=false, DeleteAsync never called",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsAdmin=false", "Status=Fixed (not Pending)", "cannot delete" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeleteReport_04 | N | Non-admin deletes own Pending report → success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NonAdminOwnPendingReport_ShouldDelete()
        {
            // Arrange
            var report  = MockReportRepository.GetSamplePendingReport(userId: "USER-001");
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new DeleteReportCommand
            {
                ReportId = report.Id,
                UserId   = "USER-001",
                IsAdmin  = false
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            repo.Verify(x => x.DeleteAsync(report), Times.Once);

            QACollector.LogTestCase("Report - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteReport",
                TestCaseID        = "DeleteReport_04",
                Description       = "Happy path: non-admin deletes own Pending report → DeleteAsync called, success",
                ExpectedResult    = "IsSuccess=true, Data=true, DeleteAsync Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsAdmin=false", "own report", "Status=Pending", "success" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeleteReport_05 | N | Admin deletes any report regardless of status → success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AdminDeletesFixedReport_ShouldSucceed()
        {
            // Arrange
            var report  = MockReportRepository.GetSampleFixedReport(userId: "USER-999");
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new DeleteReportCommand
            {
                ReportId = report.Id,
                UserId   = "ADMIN-001",
                IsAdmin  = true
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.DeleteAsync(report), Times.Once);

            QACollector.LogTestCase("Report - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteReport",
                TestCaseID        = "DeleteReport_05",
                Description       = "Admin bypasses ownership and status check → deletes Fixed report, success",
                ExpectedResult    = "IsSuccess=true, DeleteAsync Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsAdmin=true", "Report.Status=Fixed", "ownership skipped", "success" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // DeleteReport_06 | N | Admin deletes Rejected report of another user → success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AdminDeletesAnotherUsersReport_ShouldSucceed()
        {
            // Arrange
            var report  = MockReportRepository.GetSampleRejectedReport(userId: "USER-999");
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new DeleteReportCommand
            {
                ReportId = report.Id,
                UserId   = "ADMIN-001",
                IsAdmin  = true
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.DeleteAsync(report), Times.Once);

            QACollector.LogTestCase("Report - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteReport",
                TestCaseID        = "DeleteReport_06",
                Description       = "Admin deletes another user's Rejected report → success (no ownership check)",
                ExpectedResult    = "IsSuccess=true, DeleteAsync Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsAdmin=true", "different UserId", "Status=Rejected", "success" }
            });
        }
    }
}
