using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Application.UseCases.Exam.Queries.GetExamDetailStats;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class GetExamDetailStatsQueryHandlerTests
    {
        private static GetExamDetailStatsQueryHandler CreateHandler(Mock<IExamRepository>? repo = null)
            => new((repo ?? new Mock<IExamRepository>()).Object);

        private static ExamStatProjection GetSampleProjection() => new()
        {
            ExamId           = "EX-001",
            Title            = "Sample Exam",
            Status           = ExamStatus.Published,
            TotalParticipants = 50,
            AverageScore     = 78.567,
            TopScore         = 100,
            PdfDownloadCount = 5,
            SkillDurations   = "{\"Listening\":30,\"Reading\":40}",
            TemplateParts    = new List<TemplatePartStatProjection>
            {
                new() { Skill = QuestionSkill.Listening, QuestionFrom = 1,  QuestionTo = 10 },
                new() { Skill = QuestionSkill.Reading,   QuestionFrom = 11, QuestionTo = 30 }
            },
            QuestionNumbers  = new List<int> { 1, 2, 3, 5, 11, 15, 20 }
        };

        // TC-EXDS-01 | A | Exam not found → Failure
        [Fact]
        public async Task Handle_ExamNotFound_ShouldReturnFailure()
        {
            var mock = new Mock<IExamRepository>();
            mock.Setup(x => x.GetExamStatsByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ExamStatProjection?)null);

            var query = new GetExamDetailStatsQuery("GHOST");
            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Exam - Get Detail Stats", new TestCaseDetail
            {
                FunctionGroup = "Get Exam Detail Stats", TestCaseID = "TC-EXDS-01",
                Description = "ExamId returns null stats from repository",
                ExpectedResult = "Return Failure", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetExamStatsByIdAsync returns null" }
            });
        }

        // TC-EXDS-02 | N | Valid exam with stats → 200 with DTO
        [Fact]
        public async Task Handle_ValidExam_ShouldReturn200WithDTO()
        {
            var mock = new Mock<IExamRepository>();
            mock.Setup(x => x.GetExamStatsByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetSampleProjection());

            var query = new GetExamDetailStatsQuery("EX-001");
            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.ExamId.Should().Be("EX-001");
            result.Data.TotalParticipants.Should().Be(50);

            QACollector.LogTestCase("Exam - Get Detail Stats", new TestCaseDetail
            {
                FunctionGroup = "Get Exam Detail Stats", TestCaseID = "TC-EXDS-02",
                Description = "Valid exam ID retrieves stats DTO",
                ExpectedResult = "Return 200 with accurate stats", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid stats projection returned" }
            });
        }

        // TC-EXDS-03 | N | AverageScore is rounded to 2 decimals
        [Fact]
        public async Task Handle_AverageScore_ShouldBeRoundedTo2Decimals()
        {
            var mock = new Mock<IExamRepository>();
            mock.Setup(x => x.GetExamStatsByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetSampleProjection());

            var query = new GetExamDetailStatsQuery("EX-001");
            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.Data!.AverageScore.Should().Be(Math.Round(78.567, 2)); // 78.57

            QACollector.LogTestCase("Exam - Get Detail Stats", new TestCaseDetail
            {
                FunctionGroup = "Get Exam Detail Stats", TestCaseID = "TC-EXDS-03",
                Description = "AverageScore is rounded to 2 decimal places",
                ExpectedResult = "AverageScore = 78.57", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Math.Round(score, 2)" }
            });
        }

        // TC-EXDS-04 | N | SkillDurations correctly deserialized
        [Fact]
        public async Task Handle_ValidExam_ShouldDeserializeSkillDurations()
        {
            var mock = new Mock<IExamRepository>();
            mock.Setup(x => x.GetExamStatsByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetSampleProjection());

            var result = await CreateHandler(mock).Handle(new GetExamDetailStatsQuery("EX-001"), CancellationToken.None);

            result.Data!.SkillDurations.Should().ContainKey("Listening").WhoseValue.Should().Be(30);
            result.Data.SkillDurations.Should().ContainKey("Reading").WhoseValue.Should().Be(40);

            QACollector.LogTestCase("Exam - Get Detail Stats", new TestCaseDetail
            {
                FunctionGroup = "Get Exam Detail Stats", TestCaseID = "TC-EXDS-04",
                Description = "SkillDurations JSON is properly deserialized into a Dictionary",
                ExpectedResult = "Listening=30, Reading=40", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "JsonSerializer.Deserialize<Dictionary<string,int>>" }
            });
        }

        // TC-EXDS-05 | N | SkillQuestionCounts populated correctly
        [Fact]
        public async Task Handle_ValidExam_ShouldCalculateSkillQuestionCounts()
        {
            var mock = new Mock<IExamRepository>();
            mock.Setup(x => x.GetExamStatsByIdAsync("EX-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetSampleProjection());

            var result = await CreateHandler(mock).Handle(new GetExamDetailStatsQuery("EX-001"), CancellationToken.None);

            // QuestionNumbers: {1,2,3,5} in Listening(1-10), {11,15,20} in Reading(11-30)
            result.Data!.SkillQuestionCounts.Should().ContainKey("Listening").WhoseValue.Should().Be(4);
            result.Data.SkillQuestionCounts.Should().ContainKey("Reading").WhoseValue.Should().Be(3);

            QACollector.LogTestCase("Exam - Get Detail Stats", new TestCaseDetail
            {
                FunctionGroup = "Get Exam Detail Stats", TestCaseID = "TC-EXDS-05",
                Description = "SkillQuestionCounts aggregated correctly by matching question numbers to template part ranges",
                ExpectedResult = "Listening=4, Reading=3", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Count where QuestionNo in part range" }
            });
        }

        // TC-EXDS-06 | A | Repository exception propagates
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mock = new Mock<IExamRepository>();
            mock.Setup(x => x.GetExamStatsByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB Error"));

            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mock).Handle(new GetExamDetailStatsQuery("EX-001"), CancellationToken.None));

            QACollector.LogTestCase("Exam - Get Detail Stats", new TestCaseDetail
            {
                FunctionGroup = "Get Exam Detail Stats", TestCaseID = "TC-EXDS-06",
                Description = "Repository throws exception during stat retrieval",
                ExpectedResult = "Exception propagates", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ThrowsAsync" }
            });
        }
    }
}
