using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.Queries.GetExamDetailStats;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;
using Tokki.Application.UseCases.Exam.DTOs;

namespace Tokki.UnitTest.Application.UseCases.Exam.Queries
{
    public class GetExamDetailStatsQueryHandlerTests
    {
        private readonly Mock<IExamRepository> _repoMock = new();

        private GetExamDetailStatsQueryHandler CreateHandler()
        {
            return new GetExamDetailStatsQueryHandler(_repoMock.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-EXM-GDS-01 | A | Exam NotFound
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExamNotFound_ShouldReturnFailure()
        {
            _repoMock.Setup(x => x.GetExamStatsByIdAsync("fake", It.IsAny<CancellationToken>())).ReturnsAsync((ExamStatProjection?)null);
            var handler = CreateHandler();
            var cmd = new GetExamDetailStatsQuery("fake");

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Exam not found");

            QACollector.LogTestCase("Exam - Get Detail Stats", new TestCaseDetail
            {
                FunctionGroup = "GetExamDetailStatsQueryHandler",
                TestCaseID = "TC-EXM-GDS-01",
                Description = "Missing Exam Returns Failure immediately",
                ExpectedResult = "Return 400 Failure Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exam is null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-EXM-GDS-02 | N | Blank Skill Durations parsing safe
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_BlankSkillDurations_ShouldMapEmptyDictionary()
        {
            var raw = new ExamStatProjection { ExamId = "e1", SkillDurations = "" }; // Blank
            _repoMock.Setup(x => x.GetExamStatsByIdAsync("e1", It.IsAny<CancellationToken>())).ReturnsAsync(raw);
            var handler = CreateHandler();
            var cmd = new GetExamDetailStatsQuery("e1");

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.SkillDurations.Should().BeEmpty();

            QACollector.LogTestCase("Exam - Get Detail Stats", new TestCaseDetail
            {
                FunctionGroup = "GetExamDetailStatsQueryHandler",
                TestCaseID = "TC-EXM-GDS-02",
                Description = "Empty JSON Strings do not crash app but map to empty container",
                ExpectedResult = "Empty dict result",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SkillDurations is Empty/Null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-EXM-GDS-03 | N | Valid Skill Durations Json
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidSkillDurations_ShouldMapProperly()
        {
            var raw = new ExamStatProjection { ExamId = "e1", SkillDurations = "{\"Reading\": 30}" }; 
            _repoMock.Setup(x => x.GetExamStatsByIdAsync("e1", It.IsAny<CancellationToken>())).ReturnsAsync(raw);
            var handler = CreateHandler();
            var cmd = new GetExamDetailStatsQuery("e1");

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.SkillDurations["Reading"].Should().Be(30);

            QACollector.LogTestCase("Exam - Get Detail Stats", new TestCaseDetail
            {
                FunctionGroup = "GetExamDetailStatsQueryHandler",
                TestCaseID = "TC-EXM-GDS-03",
                Description = "Stored mapped JSON resolves correctly securely",
                ExpectedResult = "Valid mapping logic verified",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SkillDurations = Valid Dictionary" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-EXM-GDS-04 | N | Parts & Scores calculate dynamically correctly
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PartsCalculation_ShouldSumProperly()
        {
            var raw = new ExamStatProjection 
            { 
                ExamId = "e1", 
                TemplateParts = new List<TemplatePartStatProjection> 
                { 
                    new TemplatePartStatProjection { Skill = QuestionSkill.Listening, QuestionFrom = 1, QuestionTo = 2, Mark = 5 } 
                },
                QuestionNumbers = new List<int> { 1, 2 } // 2 questions matched in range
            }; 
            _repoMock.Setup(x => x.GetExamStatsByIdAsync("e1", It.IsAny<CancellationToken>())).ReturnsAsync(raw);
            var handler = CreateHandler();
            var cmd = new GetExamDetailStatsQuery("e1");

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.Data!.SkillQuestionCounts["Listening"].Should().Be(2); 
            result.Data.SkillTotalScores["Listening"].Should().Be(10); // 2 Qs * 5 mark

            QACollector.LogTestCase("Exam - Get Detail Stats", new TestCaseDetail
            {
                FunctionGroup = "GetExamDetailStatsQueryHandler",
                TestCaseID = "TC-EXM-GDS-04",
                Description = "Nested extraction logic evaluates successfully bounding properties exactly and assigning Marks",
                ExpectedResult = "Calculations correct",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid marks arrays sums successfully" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-EXM-GDS-05 | N | Averages Floating Math Rounding verified
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Averages_ShouldMathRoundCorrectly()
        {
            var raw = new ExamStatProjection { ExamId = "e1", AverageScore = 33.3333333333, AverageDurationMinutes = 55.55555 }; 
            _repoMock.Setup(x => x.GetExamStatsByIdAsync("e1", It.IsAny<CancellationToken>())).ReturnsAsync(raw);
            var handler = CreateHandler();
            var cmd = new GetExamDetailStatsQuery("e1");

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.Data!.AverageScore.Should().Be(33.33); // Rounded 2
            result.Data.AverageDurationMinutes.Should().Be(55.6); // Rounded 1

            QACollector.LogTestCase("Exam - Get Detail Stats", new TestCaseDetail
            {
                FunctionGroup = "GetExamDetailStatsQueryHandler",
                TestCaseID = "TC-EXM-GDS-05",
                Description = "Averages check round logic math limits correctly preventing format crashes",
                ExpectedResult = "Rounded Math results verify",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Floating logic verified precision" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-EXM-GDS-06 | B | Empty Template Parts List
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyParts_SkipsAggregationSafely()
        {
            var raw = new ExamStatProjection { ExamId = "e1", TemplateParts = new List<TemplatePartStatProjection>(), QuestionNumbers = new List<int> { 1, 2 } }; 
            _repoMock.Setup(x => x.GetExamStatsByIdAsync("e1", It.IsAny<CancellationToken>())).ReturnsAsync(raw);
            var handler = CreateHandler();
            var cmd = new GetExamDetailStatsQuery("e1");

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.Data!.SkillQuestionCounts.Should().BeEmpty();
            result.Data.SkillTotalScores.Should().BeEmpty();

            QACollector.LogTestCase("Exam - Get Detail Stats", new TestCaseDetail
            {
                FunctionGroup = "GetExamDetailStatsQueryHandler",
                TestCaseID = "TC-EXM-GDS-06",
                Description = "Missing nested configurations skip parts arrays without exception safely",
                ExpectedResult = "Empty skills mapping successfully done gracefully",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Parts are missing/empty logic" }
            });
        }
    }
}
