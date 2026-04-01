using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Application.UseCases.Exam.Queries.GetExamsStats;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class GetExamsStatsQueryHandlerTests
    {
        private static GetExamsStatsQueryHandler CreateHandler(Mock<IExamRepository>? repo = null)
            => new((repo ?? new Mock<IExamRepository>()).Object);

        private static ExamStatProjection GetSampleProjection(string id = "EX-001") => new()
        {
            ExamId           = id,
            Title            = "Exam " + id,
            Status           = ExamStatus.Published,
            TotalParticipants = 10,
            AverageScore     = 85.333,
            AverageDurationMinutes = 44.666,
            TopScore         = 100,
            SkillDurations   = null, // empty case
            TemplateParts    = new List<TemplatePartStatProjection>(),
            QuestionNumbers  = new List<int>()
        };

        private static Mock<IExamRepository> BuildRepoWithData()
        {
            var mock = new Mock<IExamRepository>();
            mock.Setup(x => x.GetPagedWithStatsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<ExamType?>(),
                It.IsAny<ExamStatus?>(), It.IsAny<ExamCreatorFilter>(), It.IsAny<ExamStatsSortBy>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<ExamStatProjection> { GetSampleProjection("EX-001"), GetSampleProjection("EX-002") }, 2));
            return mock;
        }

        // TC-EXSS-01 | N | Valid query returns 200
        [Fact]
        public async Task Handle_ValidQuery_ShouldReturn200()
        {
            var mock = BuildRepoWithData();
            var query = new GetExamsStatsQuery { PageNumber = 1, PageSize = 10 };

            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);

            QACollector.LogTestCase("Exam - Get Stats List", new TestCaseDetail
            {
                FunctionGroup = "Get Exams Stats", TestCaseID = "TC-EXSS-01",
                Description = "Basic paged stats query returns success",
                ExpectedResult = "Return 200, 2 items", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "2 projections returned" }
            });
        }

        // TC-EXSS-02 | N | AverageScore rounded to 2 decimals
        [Fact]
        public async Task Handle_ValidQuery_AverageScoreIsRounded()
        {
            var mock = BuildRepoWithData();
            var query = new GetExamsStatsQuery();

            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.Data!.Items.First().AverageScore.Should().Be(Math.Round(85.333, 2)); // 85.33

            QACollector.LogTestCase("Exam - Get Stats List", new TestCaseDetail
            {
                FunctionGroup = "Get Exams Stats", TestCaseID = "TC-EXSS-02",
                Description = "AverageScore rounded to 2 decimal places",
                ExpectedResult = "AverageScore = 85.33", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Math.Round(avg, 2)" }
            });
        }

        // TC-EXSS-03 | N | AverageDuration rounded to 1 decimal
        [Fact]
        public async Task Handle_ValidQuery_AverageDurationRoundedTo1Decimal()
        {
            var mock = BuildRepoWithData();
            var result = await CreateHandler(mock).Handle(new GetExamsStatsQuery(), CancellationToken.None);

            result.Data!.Items.First().AverageDurationMinutes.Should().Be(Math.Round(44.666, 1)); // 44.7

            QACollector.LogTestCase("Exam - Get Stats List", new TestCaseDetail
            {
                FunctionGroup = "Get Exams Stats", TestCaseID = "TC-EXSS-03",
                Description = "AverageDurationMinutes rounded to 1 decimal place",
                ExpectedResult = "AverageDuration = 44.7", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Math.Round(dur, 1)" }
            });
        }

        // TC-EXSS-04 | N | Null SkillDurations → empty dictionary
        [Fact]
        public async Task Handle_NullSkillDurations_ShouldReturnEmptyDictionary()
        {
            var mock = BuildRepoWithData();
            var result = await CreateHandler(mock).Handle(new GetExamsStatsQuery(), CancellationToken.None);

            result.Data!.Items.First().SkillDurations.Should().BeEmpty();

            QACollector.LogTestCase("Exam - Get Stats List", new TestCaseDetail
            {
                FunctionGroup = "Get Exams Stats", TestCaseID = "TC-EXSS-04",
                Description = "Null SkillDurations JSON results in empty Dictionary instead of null",
                ExpectedResult = "SkillDurations is empty dict", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "isNullOrEmpty → new Dictionary<>()" }
            });
        }

        // TC-EXSS-05 | N | Pagination properties on PagedResult
        [Fact]
        public async Task Handle_Pagination_ShouldCorrectlyCalculateTotalPages()
        {
            var mock = new Mock<IExamRepository>();
            mock.Setup(x => x.GetPagedWithStatsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<ExamType?>(),
                It.IsAny<ExamStatus?>(), It.IsAny<ExamCreatorFilter>(), It.IsAny<ExamStatsSortBy>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<ExamStatProjection>(), 50));

            var query = new GetExamsStatsQuery { PageNumber = 1, PageSize = 10 };
            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.Data!.TotalCount.Should().Be(50);
            result.Data.TotalPages.Should().Be(5);

            QACollector.LogTestCase("Exam - Get Stats List", new TestCaseDetail
            {
                FunctionGroup = "Get Exams Stats", TestCaseID = "TC-EXSS-05",
                Description = "50 total / pageSize 10 should yield TotalPages = 5",
                ExpectedResult = "TotalPages = 5", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Ceiling(50/10) = 5" }
            });
        }

        // TC-EXSS-06 | A | Repository throws exception
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mock = new Mock<IExamRepository>();
            mock.Setup(x => x.GetPagedWithStatsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<ExamType?>(),
                It.IsAny<ExamStatus?>(), It.IsAny<ExamCreatorFilter>(), It.IsAny<ExamStatsSortBy>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB Down"));

            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mock).Handle(new GetExamsStatsQuery(), CancellationToken.None));

            QACollector.LogTestCase("Exam - Get Stats List", new TestCaseDetail
            {
                FunctionGroup = "Get Exams Stats", TestCaseID = "TC-EXSS-06",
                Description = "Repository throws; exception propagates unhandled",
                ExpectedResult = "Exception propagates", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ThrowsAsync" }
            });
        }
    }
}
