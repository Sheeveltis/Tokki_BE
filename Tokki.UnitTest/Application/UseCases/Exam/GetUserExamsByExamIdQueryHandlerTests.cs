using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Application.UseCases.Exam.Queries.GetUserExamsByExamId;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class GetUserExamsByExamIdQueryHandlerTests
    {
        private static GetUserExamsByExamIdQueryHandler CreateHandler(Mock<IUserExamRepository>? repo = null)
            => new((repo ?? new Mock<IUserExamRepository>()).Object);

        private static PagedResult<ExamParticipantDTO> GetSampleResult(int count = 2) =>
            PagedResult<ExamParticipantDTO>.Create(new List<ExamParticipantDTO>
            {
                new() { UserEmail = "alice@test.com", UserName = "Alice" },
                new() { UserEmail = "bob@test.com",   UserName = "Bob" }
            }, count, 1, 10);

        // TC-EXUE-01 | N | Valid query returns 200 with participants
        [Fact]
        public async Task Handle_ValidQuery_ShouldReturn200WithParticipants()
        {
            var mock = new Mock<IUserExamRepository>();
            mock.Setup(x => x.GetPagedParticipantsByExamIdAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<ExamParticipantSortBy>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GetSampleResult());

            var query = new GetUserExamsByExamIdQuery { ExamId = "EX-001", PageNumber = 1, PageSize = 10 };
            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);

            QACollector.LogTestCase("Exam - Get User Exams", new TestCaseDetail
            {
                FunctionGroup = "Get User Exams By ExamId", TestCaseID = "TC-EXUE-01",
                Description = "Valid query returns participant list",
                ExpectedResult = "Return 200 with 2 participants", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "2 participants returned from repo" }
            });
        }

        // TC-EXUE-02 | N | Correct DTO data mapping
        [Fact]
        public async Task Handle_ValidQuery_ShouldMapParticipantsCorrectly()
        {
            var mock = new Mock<IUserExamRepository>();
            mock.Setup(x => x.GetPagedParticipantsByExamIdAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<ExamParticipantSortBy>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GetSampleResult());

            var result = await CreateHandler(mock).Handle(new GetUserExamsByExamIdQuery { ExamId = "EX-001" }, CancellationToken.None);

            result.Data!.Items.Should().Contain(p => p.UserEmail == "alice@test.com" && p.UserName == "Alice");

            QACollector.LogTestCase("Exam - Get User Exams", new TestCaseDetail
            {
                FunctionGroup = "Get User Exams By ExamId", TestCaseID = "TC-EXUE-02",
                Description = "Verify data fields in participant DTOs are correctly mapped",
                ExpectedResult = "UserId, FullName mapped from repository result", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DTO field verification" }
            });
        }

        // TC-EXUE-03 | N | Empty participants list returns 200 with empty items
        [Fact]
        public async Task Handle_NoParticipants_ShouldReturn200WithEmptyList()
        {
            var mock = new Mock<IUserExamRepository>();
            mock.Setup(x => x.GetPagedParticipantsByExamIdAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<ExamParticipantSortBy>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(PagedResult<ExamParticipantDTO>.Create(new List<ExamParticipantDTO>(), 0, 1, 10));

            var result = await CreateHandler(mock).Handle(new GetUserExamsByExamIdQuery { ExamId = "EX-001" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Exam - Get User Exams", new TestCaseDetail
            {
                FunctionGroup = "Get User Exams By ExamId", TestCaseID = "TC-EXUE-03",
                Description = "No participants for this exam; returns 200 with empty list",
                ExpectedResult = "Empty list, 200 OK", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repo returns empty PagedResult" }
            });
        }

        // TC-EXUE-04 | N | Paging metadata is accurate
        [Fact]
        public async Task Handle_Pagination_ShouldReturnCorrectPageInfo()
        {
            var mock = new Mock<IUserExamRepository>();
            mock.Setup(x => x.GetPagedParticipantsByExamIdAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<ExamParticipantSortBy>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(PagedResult<ExamParticipantDTO>.Create(new List<ExamParticipantDTO>(), 30, 3, 10));

            var query = new GetUserExamsByExamIdQuery { ExamId = "EX-001", PageNumber = 3, PageSize = 10 };
            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.Data!.TotalCount.Should().Be(30);
            result.Data.TotalPages.Should().Be(3);
            result.Data.PageNumber.Should().Be(3);

            QACollector.LogTestCase("Exam - Get User Exams", new TestCaseDetail
            {
                FunctionGroup = "Get User Exams By ExamId", TestCaseID = "TC-EXUE-04",
                Description = "Verify paging metadata from PagedResult",
                ExpectedResult = "TotalCount=30, TotalPages=3, PageNumber=3", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "30 total / 10 per page = 3 pages" }
            });
        }

        // TC-EXUE-05 | N | Repo called exactly once with correct ExamId
        [Fact]
        public async Task Handle_ValidQuery_RepoCalledOnceWithExamId()
        {
            var mock = new Mock<IUserExamRepository>();
            mock.Setup(x => x.GetPagedParticipantsByExamIdAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<ExamParticipantSortBy>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GetSampleResult());

            var query = new GetUserExamsByExamIdQuery { ExamId = "EX-999" };
            await CreateHandler(mock).Handle(query, CancellationToken.None);

            mock.Verify(x => x.GetPagedParticipantsByExamIdAsync(
                "EX-999", It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<ExamParticipantSortBy>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Exam - Get User Exams", new TestCaseDetail
            {
                FunctionGroup = "Get User Exams By ExamId", TestCaseID = "TC-EXUE-05",
                Description = "Repo is invoked once with the exact ExamId from the query",
                ExpectedResult = "GetPagedParticipantsByExamIdAsync called once with EX-999", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Times.Once, ExamId match" }
            });
        }

        // TC-EXUE-06 | A | Repository throws exception
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mock = new Mock<IUserExamRepository>();
            mock.Setup(x => x.GetPagedParticipantsByExamIdAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<ExamParticipantSortBy>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB Unavailable"));

            await Assert.ThrowsAsync<Exception>(() =>
                CreateHandler(mock).Handle(new GetUserExamsByExamIdQuery { ExamId = "EX-001" }, CancellationToken.None));

            QACollector.LogTestCase("Exam - Get User Exams", new TestCaseDetail
            {
                FunctionGroup = "Get User Exams By ExamId", TestCaseID = "TC-EXUE-06",
                Description = "Repository throws exception; propagates without suppression",
                ExpectedResult = "Exception propagates", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ThrowsAsync" }
            });
        }
    }
}
