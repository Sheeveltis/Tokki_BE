using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Application.UseCases.UserExam.Queries.GetExamAnalysis;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class GetExamAnalysisQueryHandlerTests
    {
        private static GetExamAnalysisQueryHandler CreateHandler(Mock<IUserExamRepository>? repo = null)
            => new GetExamAnalysisQueryHandler((repo ?? new Mock<IUserExamRepository>()).Object);

        private static Domain.Entities.UserExam BuildSession(
            string id = "UE-001",
            UserExamStatus status = UserExamStatus.Completed)
            => new Domain.Entities.UserExam
            {
                UserExamId = id,
                Status     = status
            };

        // ═══════════════════════════════════════════════════════════════
        // TC-GEAN-01 | A | Session not found → 404
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionNotFound_ShouldReturn404()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.UserExam?)null);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetExamAnalysisQuery("INVALID"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Analysis", new TestCaseDetail
            {
                FunctionGroup     = "GetExamAnalysis",
                TestCaseID        = "TC-GEAN-01",
                Description       = "UserExamId not found → 404",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GEAN-02 | A | Session still InProgress → 400
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SessionInProgress_ShouldReturn400()
        {
            // Arrange
            var session = BuildSession(status: UserExamStatus.InProgress);
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetExamAnalysisQuery("UE-001"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Analysis", new TestCaseDetail
            {
                FunctionGroup     = "GetExamAnalysis",
                TestCaseID        = "TC-GEAN-02",
                Description       = "Exam not submitted (InProgress) → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status=InProgress" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GEAN-03 | N | No analysis data → 200 with empty analysis lists
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoAnalysisData_ShouldReturn200WithEmptyAnalysis()
        {
            // Arrange
            var session = BuildSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            repo.Setup(x => x.GetExamAnalysisSummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuestionTypeDto>());
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetExamAnalysisQuery("UE-001"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.ReadingAnalysis.Should().BeEmpty();
            result.Data.ListeningAnalysis.Should().BeEmpty();

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Analysis", new TestCaseDetail
            {
                FunctionGroup     = "GetExamAnalysis",
                TestCaseID        = "TC-GEAN-03",
                Description       = "No analysis data → 200 with empty analysis lists",
                ExpectedResult    = "IsSuccess=true, all lists empty",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetExamAnalysisSummaryAsync returns empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GEAN-04 | N | Analysis data grouped by skill correctly
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AnalysisData_ShouldGroupBySkill()
        {
            // Arrange
            var session = BuildSession();
            var analysisData = new List<QuestionTypeDto>
            {
                new QuestionTypeDto { Skill = QuestionSkill.Reading,   Name = "Grammar" },
                new QuestionTypeDto { Skill = QuestionSkill.Listening, Name = "Dialogue" },
                new QuestionTypeDto { Skill = QuestionSkill.Writing,   Name = "Essay" }
            };
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            repo.Setup(x => x.GetExamAnalysisSummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisData);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(new GetExamAnalysisQuery("UE-001"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.ReadingAnalysis.Should().HaveCount(1);
            result.Data.ListeningAnalysis.Should().HaveCount(1);
            result.Data.WritingAnalysis.Should().HaveCount(1);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Analysis", new TestCaseDetail
            {
                FunctionGroup     = "GetExamAnalysis",
                TestCaseID        = "TC-GEAN-04",
                Description       = "3 types (R/L/W) → each grouped into correct list",
                ExpectedResult    = "Each list has 1 item",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "1 QuestionTypeDto per skill" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GEAN-05 | N | GetExamAnalysisSummaryAsync called with correct UserExamId
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldCallAnalysisRepoWithCorrectId()
        {
            // Arrange
            var session = BuildSession();
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync("UE-001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            repo.Setup(x => x.GetExamAnalysisSummaryAsync("UE-001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<QuestionTypeDto>());
            var handler = CreateHandler(repo);

            // Act
            await handler.Handle(new GetExamAnalysisQuery("UE-001"), CancellationToken.None);

            // Assert
            repo.Verify(x => x.GetExamAnalysisSummaryAsync("UE-001", It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Analysis", new TestCaseDetail
            {
                FunctionGroup     = "GetExamAnalysis",
                TestCaseID        = "TC-GEAN-05",
                Description       = "GetExamAnalysisSummaryAsync called once with correct UserExamId",
                ExpectedResult    = "Times.Once with 'UE-001'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserExamId forwarded correctly" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GEAN-06 | E | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB failure"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(new GetExamAnalysisQuery("UE-001"), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB failure");

            // Excel Log
            QACollector.LogTestCase("UserExam - Get Analysis", new TestCaseDetail
            {
                FunctionGroup     = "GetExamAnalysis",
                TestCaseID        = "TC-GEAN-06",
                Description       = "Repository throws exception → propagates",
                ExpectedResult    = "Exception with 'DB failure'",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync throws Exception" }
            });
        }
    }
}
