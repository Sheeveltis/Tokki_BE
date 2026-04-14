using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Reports.Commands.UpdateReportStatus;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Reports
{
    public class UpdateReportStatusHandlerTests
    {
        private static UpdateReportStatusHandler CreateHandler(Mock<IReportRepository>? repo = null)
            => new UpdateReportStatusHandler((repo ?? MockReportRepository.GetMock()).Object);

        // ═══════════════════════════════════════════════════════════
        // TC-RPT-UPD-01 | A | Report not found → failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ReportNotFound_ShouldReturnFailure()
        {
            // Arrange
            var repo    = MockReportRepository.GetMock(returnedById: null);
            var handler = CreateHandler(repo);
            var command = new UpdateReportStatusCommand { ReportId = "RPT-MISSING", NewStatus = ReportStatus.Fixed };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Report - Update Status", new TestCaseDetail
            {
                FunctionGroup     = "UpdateReportStatus",
                TestCaseID        = "TC-RPT-UPD-01",
                Description       = "Report not found → ReportNotFound failure",
                ExpectedResult    = "IsSuccess=false",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "failure" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPT-UPD-02 | N | Status=Fixed → ResolvedAt set, UserHasRead=false
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_StatusFixed_ShouldSetResolvedAtAndUserHasReadFalse()
        {
            // Arrange
            var report  = MockReportRepository.GetSamplePendingReport();
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var before  = DateTime.UtcNow.AddSeconds(-1);
            var command = new UpdateReportStatusCommand
            {
                ReportId   = report.Id,
                NewStatus  = ReportStatus.Fixed,
                AdminReply = "Issue has been resolved"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            report.Status.Should().Be(ReportStatus.Fixed);
            report.UserHasRead.Should().BeFalse();
            report.ResolvedAt.Should().NotBeNull();
            report.ResolvedAt.Should().BeAfter(before);
            report.AdminReply.Should().Be("Issue has been resolved");

            QACollector.LogTestCase("Report - Update Status", new TestCaseDetail
            {
                FunctionGroup     = "UpdateReportStatus",
                TestCaseID        = "TC-RPT-UPD-02",
                Description       = "Status=Fixed → ResolvedAt set to UtcNow, UserHasRead=false, AdminReply stored",
                ExpectedResult    = "IsSuccess=true, Status=Fixed, UserHasRead=false, ResolvedAt set",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "NewStatus=Fixed", "ResolvedAt and UserHasRead=false set" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPT-UPD-03 | N | Status=Rejected → ResolvedAt set, UserHasRead=false
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_StatusRejected_ShouldSetResolvedAtAndUserHasReadFalse()
        {
            // Arrange
            var report  = MockReportRepository.GetSamplePendingReport();
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new UpdateReportStatusCommand
            {
                ReportId  = report.Id,
                NewStatus = ReportStatus.Rejected,
                AdminReply = "Not a valid bug"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            report.Status.Should().Be(ReportStatus.Rejected);
            report.UserHasRead.Should().BeFalse();
            report.ResolvedAt.Should().NotBeNull();

            QACollector.LogTestCase("Report - Update Status", new TestCaseDetail
            {
                FunctionGroup     = "UpdateReportStatus",
                TestCaseID        = "TC-RPT-UPD-03",
                Description       = "Status=Rejected → ResolvedAt set, UserHasRead=false (notification to user)",
                ExpectedResult    = "IsSuccess=true, Status=Rejected, UserHasRead=false, ResolvedAt set",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "NewStatus=Rejected", "ResolvedAt and UserHasRead=false set" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPT-UPD-04 | N | Status=Processing → UserHasRead=true, no ResolvedAt
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_StatusProcessing_ShouldSetUserHasReadTrueAndNoResolvedAt()
        {
            // Arrange
            var report  = MockReportRepository.GetSamplePendingReport();
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new UpdateReportStatusCommand
            {
                ReportId  = report.Id,
                NewStatus = ReportStatus.Processing
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            report.Status.Should().Be(ReportStatus.Processing);
            report.UserHasRead.Should().BeTrue(); // not fixed/rejected → userHasRead=true
            report.ResolvedAt.Should().BeNull();

            QACollector.LogTestCase("Report - Update Status", new TestCaseDetail
            {
                FunctionGroup     = "UpdateReportStatus",
                TestCaseID        = "TC-RPT-UPD-04",
                Description       = "Status=Processing → UserHasRead=true (else branch), ResolvedAt NOT set",
                ExpectedResult    = "IsSuccess=true, Status=Processing, UserHasRead=true, ResolvedAt=null",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "NewStatus=Processing (not Fixed/Rejected)", "else branch: UserHasRead=true" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPT-UPD-05 | B | AdminReply stored in report regardless of status
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WithAdminReply_ShouldStoreAdminReply()
        {
            // Arrange
            var report  = MockReportRepository.GetSamplePendingReport();
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new UpdateReportStatusCommand
            {
                ReportId   = report.Id,
                NewStatus  = ReportStatus.Processing,
                AdminReply = "We are investigating"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            report.AdminReply.Should().Be("We are investigating");
            repo.Verify(x => x.UpdateAsync(It.IsAny<Report>()), Times.Once);

            QACollector.LogTestCase("Report - Update Status", new TestCaseDetail
            {
                FunctionGroup     = "UpdateReportStatus",
                TestCaseID        = "TC-RPT-UPD-05",
                Description       = "AdminReply stored in report entity, UpdateAsync called once",
                ExpectedResult    = "entity.AdminReply='We are investigating', UpdateAsync Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AdminReply provided", "stored on entity" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPT-UPD-06 | B | UpdateAsync called with the exact updated report
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCommand_UpdateAsyncCalledWithUpdatedStatus()
        {
            // Arrange
            var report  = MockReportRepository.GetSamplePendingReport();
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new UpdateReportStatusCommand
            {
                ReportId  = report.Id,
                NewStatus = ReportStatus.Fixed
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            repo.Verify(x => x.UpdateAsync(It.Is<Report>(r => r.Status == ReportStatus.Fixed)), Times.Once);

            QACollector.LogTestCase("Report - Update Status", new TestCaseDetail
            {
                FunctionGroup     = "UpdateReportStatus",
                TestCaseID        = "TC-RPT-UPD-06",
                Description       = "Boundary: UpdateAsync called with entity.Status=Fixed",
                ExpectedResult    = "UpdateAsync(entity.Status=Fixed) Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status set to Fixed before Update", "verified in mock" }
            });
        }
    }
}
