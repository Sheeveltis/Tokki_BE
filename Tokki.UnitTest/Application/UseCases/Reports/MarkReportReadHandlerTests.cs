using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Reports.Commands.MarkReportRead;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Reports
{
    public class MarkReportReadHandlerTests
    {
        private static MarkReportReadHandler CreateHandler(Mock<IReportRepository>? repo = null)
            => new MarkReportReadHandler((repo ?? MockReportRepository.GetMock()).Object);

        // ═══════════════════════════════════════════════════════════
        // TC-RPT-MRR-01 | A | Report not found → failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ReportNotFound_ShouldReturnFailure()
        {
            // Arrange
            var repo    = MockReportRepository.GetMock(returnedById: null);
            var handler = CreateHandler(repo);
            var command = new MarkReportReadCommand { ReportId = "RPT-MISSING", UserId = "USER-001" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Report - Mark Read", new TestCaseDetail
            {
                FunctionGroup     = "MarkReportRead",
                TestCaseID        = "TC-RPT-MRR-01",
                Description       = "Report not found → ReportNotFound failure",
                ExpectedResult    = "IsSuccess=false",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "failure" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPT-MRR-02 | A | UserId does not match report owner → unauthorized
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WrongUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var report  = MockReportRepository.GetSampleFixedReport(userId: "OWNER-001");
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new MarkReportReadCommand
            {
                ReportId = report.Id,
                UserId   = "OTHER-USER"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            repo.Verify(x => x.UpdateAsync(It.IsAny<Report>()), Times.Never);

            QACollector.LogTestCase("Report - Mark Read", new TestCaseDetail
            {
                FunctionGroup     = "MarkReportRead",
                TestCaseID        = "TC-RPT-MRR-02",
                Description       = "UserId does not match report.UserId → ReportUnauthorized failure",
                ExpectedResult    = "IsSuccess=false, UpdateAsync never called",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserId != report.UserId", "unauthorized, no update" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPT-MRR-03 | N | Happy path: owner marks read → UserHasRead=true, success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_OwnerMarksRead_ShouldSetUserHasReadAndReturnSuccess()
        {
            // Arrange
            var report  = MockReportRepository.GetSampleFixedReport(userId: "USER-001");
            report.UserHasRead = false; // unread
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new MarkReportReadCommand { ReportId = report.Id, UserId = "USER-001" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            report.UserHasRead.Should().BeTrue();
            repo.Verify(x => x.UpdateAsync(report), Times.Once);

            QACollector.LogTestCase("Report - Mark Read", new TestCaseDetail
            {
                FunctionGroup     = "MarkReportRead",
                TestCaseID        = "TC-RPT-MRR-03",
                Description       = "Happy path: owner marks Fixed report as read → UserHasRead=true, UpdateAsync called, success",
                ExpectedResult    = "IsSuccess=true, Data=true, report.UserHasRead=true, UpdateAsync Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Owner's report", "UserHasRead was false", "set to true" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPT-MRR-04 | N | Already read → still calls UpdateAsync (idempotent)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AlreadyRead_ShouldStillCallUpdateAndReturnSuccess()
        {
            // Arrange
            var report  = MockReportRepository.GetSamplePendingReport(userId: "USER-001");
            report.UserHasRead = true; // already read
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new MarkReportReadCommand { ReportId = report.Id, UserId = "USER-001" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            report.UserHasRead.Should().BeTrue();
            repo.Verify(x => x.UpdateAsync(report), Times.Once); // still updates

            QACollector.LogTestCase("Report - Mark Read", new TestCaseDetail
            {
                FunctionGroup     = "MarkReportRead",
                TestCaseID        = "TC-RPT-MRR-04",
                Description       = "Report already read → handler still calls UpdateAsync (idempotent), success",
                ExpectedResult    = "IsSuccess=true, UpdateAsync Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserHasRead already true", "UpdateAsync still called once" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPT-MRR-05 | B | UpdateAsync called with exact report entity
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCommand_UpdateAsyncCalledWithCorrectEntity()
        {
            // Arrange
            var report  = MockReportRepository.GetSampleRejectedReport(userId: "USER-001");
            report.UserHasRead = false;
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new MarkReportReadCommand { ReportId = report.Id, UserId = "USER-001" };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            repo.Verify(x => x.UpdateAsync(It.Is<Report>(r => r.UserHasRead == true)), Times.Once);

            QACollector.LogTestCase("Report - Mark Read", new TestCaseDetail
            {
                FunctionGroup     = "MarkReportRead",
                TestCaseID        = "TC-RPT-MRR-05",
                Description       = "Boundary: UpdateAsync called with entity where UserHasRead=true",
                ExpectedResult    = "UpdateAsync(entity.UserHasRead=true) Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserHasRead set before Update call" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-RPT-MRR-06 | A | GetByIdAsync called with correct ReportId
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCommand_GetByIdCalledWithCorrectReportId()
        {
            // Arrange
            const string rptId = "RPT-SPECIFIC-01";
            var report  = MockReportRepository.GetSampleFixedReport(id: rptId, userId: "USER-001");
            var repo    = MockReportRepository.GetMock(returnedById: report);
            var handler = CreateHandler(repo);
            var command = new MarkReportReadCommand { ReportId = rptId, UserId = "USER-001" };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            repo.Verify(x => x.GetByIdAsync(rptId), Times.Once);

            QACollector.LogTestCase("Report - Mark Read", new TestCaseDetail
            {
                FunctionGroup     = "MarkReportRead",
                TestCaseID        = "TC-RPT-MRR-06",
                Description       = "Boundary: GetByIdAsync called with exact ReportId from command",
                ExpectedResult    = "GetByIdAsync('RPT-SPECIFIC-01') Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Specific ReportId", "GetByIdAsync called once with that id" }
            });
        }
    }
}
