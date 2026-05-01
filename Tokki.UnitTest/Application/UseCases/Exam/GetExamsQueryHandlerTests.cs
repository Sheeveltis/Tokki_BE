using ExamEntity = Tokki.Domain.Entities.Exam;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Application.UseCases.Exam.Queries.GetExams;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class GetExamsQueryHandlerTests
    {
        private static GetExamsQueryHandler CreateHandler(Mock<IExamRepository>? repo = null)
            => new((repo ?? new Mock<IExamRepository>()).Object);

        private static IEnumerable<ExamEntity> GetSampleExams() => new List<ExamEntity>
        {
            new ExamEntity() { ExamId = "EX-001", Title = "Exam A", Status = ExamStatus.Published, Duration = 60, ExamQuestions = new List<ExamQuestion>(), ExamTemplate = new() { Name = "TOPIK I" } },
            new ExamEntity() { ExamId = "EX-002", Title = "Exam B", Status = ExamStatus.Draft,     Duration = 90, ExamQuestions = new List<ExamQuestion>(), ExamTemplate = new() { Name = "TOPIK II" } }
        };

        private static Mock<IExamRepository> BuildRepoWithData()
        {
            var mock = new Mock<IExamRepository>();
            mock.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<ExamType?>(),
                It.IsAny<ExamStatus?>(), It.IsAny<string?>(), It.IsAny<ExamCreatorFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetSampleExams(), 2));
            return mock;
        }

        // Get_Exams_01 | N | Valid query returns 200 with data
        [Fact]
        public async Task Handle_ValidQuery_ShouldReturn200WithItems()
        {
            var mock = BuildRepoWithData();
            var query = new GetExamsQuery { PageNumber = 1, PageSize = 10 };

            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);

            QACollector.LogTestCase("Exam - Get List", new TestCaseDetail
            {
                FunctionGroup = "Get Exams", TestCaseID = "Get_Exams_01",
                Description = "Basic paged query retrieval",
                ExpectedResult = "Return 200 with 2 items", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "2 exams in DB" }
            });
        }

        // Get_Exams_02 | N | DTOs mapped correctly
        [Fact]
        public async Task Handle_ValidQuery_ShouldMapDTOsCorrectly()
        {
            var mock = BuildRepoWithData();
            var query = new GetExamsQuery();

            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.Data!.Items.First().ExamId.Should().Be("EX-001");
            result.Data.Items.First().Title.Should().Be("Exam A");
            result.Data.Items.First().ExamTemplateName.Should().Be("TOPIK I");

            QACollector.LogTestCase("Exam - Get List", new TestCaseDetail
            {
                FunctionGroup = "Get Exams", TestCaseID = "Get_Exams_02",
                Description = "Verify DTO mapping from entity",
                ExpectedResult = "ExamId, Title, ExamTemplateName correctly mapped", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DTO fields verify" }
            });
        }

        // Get_Exams_03 | N | Pagination
        [Fact]
        public async Task Handle_Pagination_ShouldReturnCorrectPageInfo()
        {
            var mock = new Mock<IExamRepository>();
            mock.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<ExamType?>(),
                It.IsAny<ExamStatus?>(), It.IsAny<string?>(), It.IsAny<ExamCreatorFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetSampleExams(), 20));

            var query = new GetExamsQuery { PageNumber = 2, PageSize = 10 };
            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.Data!.TotalCount.Should().Be(20);
            result.Data.PageNumber.Should().Be(2);
            result.Data.TotalPages.Should().Be(2);

            QACollector.LogTestCase("Exam - Get List", new TestCaseDetail
            {
                FunctionGroup = "Get Exams", TestCaseID = "Get_Exams_03",
                Description = "Verify paging metadata on result",
                ExpectedResult = "TotalCount 20, PageNumber 2, TotalPages 2", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "20 total / pageSize 10 = 2 pages" }
            });
        }

        // Get_Exams_04 | N | Empty DB returns empty page
        [Fact]
        public async Task Handle_EmptyDatabase_ShouldReturnEmptyResult()
        {
            var mock = new Mock<IExamRepository>();
            mock.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<ExamType?>(),
                It.IsAny<ExamStatus?>(), It.IsAny<string?>(), It.IsAny<ExamCreatorFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<ExamEntity>(), 0));

            var query = new GetExamsQuery();
            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Exam - Get List", new TestCaseDetail
            {
                FunctionGroup = "Get Exams", TestCaseID = "Get_Exams_04",
                Description = "Empty database returns graceful empty result",
                ExpectedResult = "Items empty, TotalCount 0, 200 OK", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repo returns empty list" }
            });
        }

        // Get_Exams_05 | N | Repo called once with exact params
        [Fact]
        public async Task Handle_ValidQuery_RepoCalledOnce()
        {
            var mock = BuildRepoWithData();
            var query = new GetExamsQuery { PageNumber = 1, PageSize = 5, Status = ExamStatus.Published };

            await CreateHandler(mock).Handle(query, CancellationToken.None);

            mock.Verify(x => x.GetPagedAsync(
                1, 5, null, null, ExamStatus.Published, null, ExamCreatorFilter.All, default), Times.Once);

            QACollector.LogTestCase("Exam - Get List", new TestCaseDetail
            {
                FunctionGroup = "Get Exams", TestCaseID = "Get_Exams_05",
                Description = "Verify exact params routed to repository once",
                ExpectedResult = "Times.Once verification passes", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Mock.Verify strict parameter match" }
            });
        }

        // Get_Exams_06 | A | Exception propagates
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mock = new Mock<IExamRepository>();
            mock.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<ExamType?>(),
                It.IsAny<ExamStatus?>(), It.IsAny<string?>(), It.IsAny<ExamCreatorFilter>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB Down"));

            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mock).Handle(new GetExamsQuery(), CancellationToken.None));

            QACollector.LogTestCase("Exam - Get List", new TestCaseDetail
            {
                FunctionGroup = "Get Exams", TestCaseID = "Get_Exams_06",
                Description = "Repository throws unhandled exception",
                ExpectedResult = "Exception propagates", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ThrowsAsync, no try/catch" }
            });
        }
    }
}
