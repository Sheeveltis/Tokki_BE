using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Reports.Commands.CreateReport;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Reports
{
    public class CreateReportHandlerTests
    {
        private static Mock<IIdGeneratorService> GetIdGenMock(string id = "RPT-GEN-001")
        {
            var mock = new Mock<IIdGeneratorService>();
            mock.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns(id);
            return mock;
        }

        private static CreateReportHandler CreateHandler(
            Mock<IReportRepository>?   repo  = null,
            Mock<IIdGeneratorService>? idGen = null)
        {
            return new CreateReportHandler(
                (repo  ?? MockReportRepository.GetMock()).Object,
                (idGen ?? GetIdGenMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // CreateReport_01 | N | Happy path → report created, 200 success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateReportAndReturnSuccessWithId()
        {
            // Arrange
            var repo    = MockReportRepository.GetMock();
            var idGen   = GetIdGenMock("RPT-GEN-001");
            var handler = CreateHandler(repo, idGen);
            var command = new CreateReportCommand
            {
                UserId      = "USER-001",
                Description = "Found a bug in question display",
                ReportType  = "Bug",
                TargetUrl   = "/questions/QB-001"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("RPT-GEN-001");
            repo.Verify(x => x.AddAsync(It.Is<Report>(r =>
                r.UserId == "USER-001" &&
                r.Status == ReportStatus.Pending &&
                r.UserHasRead == true)), Times.Once);

            QACollector.LogTestCase("Report - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateReport",
                TestCaseID        = "CreateReport_01",
                Description       = "Happy path: valid command → Report created with Pending status, UserHasRead=true, returns report Id",
                ExpectedResult    = "IsSuccess=true, Data='RPT-GEN-001', Status=Pending",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid command", "AddAsync called", "Status=Pending, UserHasRead=true" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateReport_02 | N | With optional QuestionBankId → stored on entity
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WithQuestionBankId_ShouldStoreQuestionBankIdOnReport()
        {
            // Arrange
            var repo    = MockReportRepository.GetMock();
            var handler = CreateHandler(repo);
            var command = new CreateReportCommand
            {
                UserId         = "USER-002",
                Description    = "Wrong answer for QB-001",
                ReportType     = "Question",
                QuestionBankId = "QB-001"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.AddAsync(It.Is<Report>(r => r.QuestionBankId == "QB-001")), Times.Once);

            QACollector.LogTestCase("Report - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateReport",
                TestCaseID        = "CreateReport_02",
                Description       = "QuestionBankId provided → stored on entity in AddAsync",
                ExpectedResult    = "IsSuccess=true, entity.QuestionBankId='QB-001'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionBankId='QB-001'", "stored in Report entity" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateReport_03 | N | Generated ID is assigned to report
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_GeneratedIdAssignedToReport()
        {
            // Arrange
            const string fixedId = "RPT-FIXED-ID";
            var idGen   = GetIdGenMock(fixedId);
            var repo    = MockReportRepository.GetMock();
            var handler = CreateHandler(repo, idGen);
            var command = new CreateReportCommand
            {
                UserId = "USER-001", Description = "Test", ReportType = "Bug"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Data.Should().Be(fixedId);
            repo.Verify(x => x.AddAsync(It.Is<Report>(r => r.Id == fixedId)), Times.Once);

            QACollector.LogTestCase("Report - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateReport",
                TestCaseID        = "CreateReport_03",
                Description       = "Boundary: generated Id assigned to entity.Id and returned in Data",
                ExpectedResult    = "Data='RPT-FIXED-ID', entity.Id='RPT-FIXED-ID'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IdGenerator returns fixed id", "assigned to report" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateReport_04 | A | Repository AddAsync throws → 500 failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturnFailure()
        {
            // Arrange
            var repo = new Mock<IReportRepository>();
            repo.Setup(x => x.AddAsync(It.IsAny<Report>()))
                .ThrowsAsync(new Exception("DB connection lost"));
            var handler = CreateHandler(repo);
            var command = new CreateReportCommand
            {
                UserId = "USER-001", Description = "Test", ReportType = "Bug"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Report - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateReport",
                TestCaseID        = "CreateReport_04",
                Description       = "AddAsync throws → caught in try/catch → failure returned",
                ExpectedResult    = "IsSuccess=false (AppErrors.ReportCreationFailed)",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AddAsync throws", "catch returns failure" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateReport_05 | N | CreatedAt is set (not default DateTime)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidCommand_CreatedAtShouldBeSet()
        {
            // Arrange
            var repo    = MockReportRepository.GetMock();
            var handler = CreateHandler(repo);
            var before  = DateTime.UtcNow.AddSeconds(-1);
            var command = new CreateReportCommand
            {
                UserId = "USER-001", Description = "Test", ReportType = "Bug"
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            repo.Verify(x => x.AddAsync(It.Is<Report>(r =>
                r.CreatedAt >= before && r.CreatedAt <= DateTime.UtcNow.AddSeconds(1))), Times.Once);

            QACollector.LogTestCase("Report - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateReport",
                TestCaseID        = "CreateReport_05",
                Description       = "Boundary: entity.CreatedAt is set to approximately DateTime.UtcNow",
                ExpectedResult    = "entity.CreatedAt within valid time range",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "CreatedAt set in handler", "within expected range" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateReport_06 | N | Null optional fields → stored as null
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullOptionalFields_ShouldStoreNulls()
        {
            // Arrange
            var repo    = MockReportRepository.GetMock();
            var handler = CreateHandler(repo);
            var command = new CreateReportCommand
            {
                UserId         = "USER-001",
                Description    = "Minimal report",
                ReportType     = "Other",
                ImageUrl       = null,
                TargetUrl      = null,
                QuestionBankId = null,
                VocabularyId   = null
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.AddAsync(It.Is<Report>(r =>
                r.ImageUrl == null && r.QuestionBankId == null && r.VocabularyId == null)), Times.Once);

            QACollector.LogTestCase("Report - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateReport",
                TestCaseID        = "CreateReport_06",
                Description       = "Null optional fields (ImageUrl, QuestionBankId, VocabularyId) → stored as null on entity",
                ExpectedResult    = "IsSuccess=true, optional fields null on entity",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Optional fields null", "stored as null in Report" }
            });
        }
    }
}
