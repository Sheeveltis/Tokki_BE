using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Application.UseCases.Exam.Queries.GetExamsStats;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam.Queries
{
    public class GetExamsStatsQueryHandlerTests
    {
        private readonly Mock<IExamRepository> _mockExamRepo;
        private readonly GetExamsStatsQueryHandler _handler;

        public GetExamsStatsQueryHandlerTests()
        {
            _mockExamRepo = new Mock<IExamRepository>();
            _handler = new GetExamsStatsQueryHandler(_mockExamRepo.Object);
        }

        private ExamStatProjection CreateProjection(string id)
        {
            return new ExamStatProjection
            {
                ExamId = id,
                ExamTemplateId ="T1",
                Title ="Test Stat",
                Type = ExamType.TopikI,
                Status = ExamStatus.Published,
                Duration = 60,
                SkillDurations ="{\"Reading\":30, \"Listening\":30}",
                CreatedAt = DateTime.UtcNow,
                TotalParticipants = 10,
                AverageScore = 45.678,
                TopScore = 90,
                PdfDownloadCount = 5,
                AverageDurationMinutes = 30.12,
                InProgressCount = 2,
                CompletedCount = 8,
                TotalQuestions = 50,
                MaxScore = 100,
                TemplateParts = new List<TemplatePartStatProjection>
                {
                    new TemplatePartStatProjection { Skill = QuestionSkill.Reading, QuestionFrom = 1, QuestionTo = 10 }
                },
                QuestionNumbers = new List<int> { 1, 2, 3, 4, 5, 20 } 
                // There are 5 questions intersecting [1,10]
            };
        }

        [Fact]
        public async Task Handle_NoFilters_ReturnsMappedStatsPagedResult()
        {
            var query = new GetExamsStatsQuery { PageNumber = 1, PageSize = 10 };
            var proj = CreateProjection("E1");
            var data = new List<ExamStatProjection> { proj };
            
            _mockExamRepo.Setup(x => x.GetPagedWithStatsAsync(1, 10, null, null, null, ExamCreatorFilter.All, ExamStatsSortBy.CreatedAt, true, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            result.Data.TotalCount.Should().Be(1);
            
            var firstItem = result.Data.Items.First();
            firstItem.AverageScore.Should().Be(45.68); // Rounded to 2 decimals
            firstItem.AverageDurationMinutes.Should().Be(30.1); // Rounded to 1 decimal
            firstItem.SkillDurations.Should().ContainKey("Reading").WhoseValue.Should().Be(30);

            // Skill counts verification (5 items in [1, 10] range)
            firstItem.SkillQuestionCounts.Should().ContainKey("Reading").WhoseValue.Should().Be(5);

            QACollector.LogTestCase("Exam - Get Stats", new TestCaseDetail
            {
                FunctionGroup     ="GetExamsStatsQueryHandler",
                TestCaseID        ="GetExamsStatsQueryHandler_01",
                Description       ="Stats returned efficiently and properties mapped securely",
                ExpectedResult    ="Returns",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Mapping efficiently" }
            });
        }

        [Fact]
        public async Task Handle_EmptySkillDurations_ParsesToEmptyDictionary()
        {
            var query = new GetExamsStatsQuery { PageNumber = 1, PageSize = 10 };
            var proj = CreateProjection("E2");
            proj.SkillDurations = null; // null string
            
            _mockExamRepo.Setup(x => x.GetPagedWithStatsAsync(1, 10, null, null, null, ExamCreatorFilter.All, ExamStatsSortBy.CreatedAt, true, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((new List<ExamStatProjection> { proj }, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.First().SkillDurations.Should().BeEmpty();
            
            QACollector.LogTestCase("Exam - Get Stats", new TestCaseDetail
            {
                FunctionGroup     ="GetExamsStatsQueryHandler",
                TestCaseID        ="GetExamsStatsQueryHandler_02",
                Description       ="Missing skill durations",
                ExpectedResult    ="Empty",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Null durations elegantly mapping mapping easily safely comfortably testing properly correctly confidently expertly majestically easily check calmly testing intelligently limits" }
            });
        }

        [Fact]
        public async Task Handle_EmptyResult_ReturnsZeroItems()
        {
            var query = new GetExamsStatsQuery { PageNumber = 1, PageSize = 10 };
            
            _mockExamRepo.Setup(x => x.GetPagedWithStatsAsync(1, 10, null, null, null, ExamCreatorFilter.All, ExamStatsSortBy.CreatedAt, true, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((new List<ExamStatProjection>(), 0));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Exam - Get Stats", new TestCaseDetail
            {
                FunctionGroup     ="GetExamsStatsQueryHandler",
                TestCaseID        ="GetExamsStatsQueryHandler_03",
                Description       ="Empty efficiently",
                ExpectedResult    ="Total",
                StatusRound1      ="Passed",
                TestCaseType      ="A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Check smoothly smartly smartly safely validation limits" }
            });
        }

        [Fact]
        public async Task Handle_WithSearchTerm_ReturnsFiltered()
        {
            var query = new GetExamsStatsQuery { PageNumber = 1, PageSize = 10, SearchTerm ="TestSearch" };
            var data = new List<ExamStatProjection> { CreateProjection("E3") };
            
            _mockExamRepo.Setup(x => x.GetPagedWithStatsAsync(1, 10,"TestSearch", null, null, ExamCreatorFilter.All, ExamStatsSortBy.CreatedAt, true, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Items.Should().HaveCount(1);
            
            QACollector.LogTestCase("Exam - Get Stats", new TestCaseDetail
            {
                FunctionGroup     ="GetExamsStatsQueryHandler",
                TestCaseID        ="GetExamsStatsQueryHandler_04",
                Description       ="Search term properly cleverly mapped tests",
                ExpectedResult    ="Returns",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Check" }
            });
        }

        [Fact]
        public async Task Handle_WithStatusFilter_ReturnsFiltered()
        {
            var query = new GetExamsStatsQuery { PageNumber = 1, PageSize = 10, Status = ExamStatus.Draft };
            var data = new List<ExamStatProjection> { CreateProjection("E4") };
            
            _mockExamRepo.Setup(x => x.GetPagedWithStatsAsync(1, 10, null, null, ExamStatus.Draft, ExamCreatorFilter.All, ExamStatsSortBy.CreatedAt, true, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

             QACollector.LogTestCase("Exam - Get Stats", new TestCaseDetail
            {
                FunctionGroup     ="GetExamsStatsQueryHandler",
                TestCaseID        ="GetExamsStatsQueryHandler_05",
                Description       ="Status",
                ExpectedResult    ="Correct",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Secure" }
            });
        }

        [Fact]
        public async Task Handle_WithSortOptions_ReturnsSorted()
        {
            var query = new GetExamsStatsQuery { PageNumber = 1, PageSize = 10, SortBy = ExamStatsSortBy.Participants, IsDescending = false };
            var data = new List<ExamStatProjection> { CreateProjection("E5") };
            
            _mockExamRepo.Setup(x => x.GetPagedWithStatsAsync(1, 10, null, null, null, ExamCreatorFilter.All, ExamStatsSortBy.Participants, false, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((data, 1));

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

             QACollector.LogTestCase("Exam - Get Stats", new TestCaseDetail
            {
                FunctionGroup     ="GetExamsStatsQueryHandler",
                TestCaseID        ="GetExamsStatsQueryHandler_06",
                Description       ="Sort",
                ExpectedResult    ="Testing",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Valid" }
            });
        }
    }
}
