using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Application.UseCases.UserExam.Queries.GetUserExams;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam
{
    public class GetUserExamsQueryHandlerTests
    {
        private static GetUserExamsQueryHandler CreateHandler(Mock<IUserExamRepository>? repo = null)
            => new GetUserExamsQueryHandler((repo ?? new Mock<IUserExamRepository>()).Object);

        private static Mock<IUserExamRepository> BuildRepo(
            List<UserExamActionDto>? items = null,
            int total = 0,
            int page  = 1,
            int size  = 10)
        {
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetPagedHistoryAsync(
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<UserExamStatus?>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<UserExamActionDto>(
                    items ?? new List<UserExamActionDto>(), total, page, size));
            return repo;
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GUEX-01 | N | Empty history → 200 with empty items
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoExamHistory_ShouldReturn200WithEmptyItems()
        {
            // Arrange
            var repo    = BuildRepo(items: new List<UserExamActionDto>(), total: 0);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(
                new GetUserExamsQuery { UserId = "USER-001", PageNumber = 1, PageSize = 10 },
                CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExams",
                TestCaseID        = "TC-GUEX-01",
                Description       = "No exam history → 200 with empty Items list",
                ExpectedResult    = "IsSuccess=true, Items=[], TotalCount=0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedHistoryAsync returns empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GUEX-02 | N | Two exams found → 200 with count=2
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TwoExams_ShouldReturn200WithCount2()
        {
            // Arrange
            var items = new List<UserExamActionDto>
            {
                new UserExamActionDto { UserExamId = "UE-001" },
                new UserExamActionDto { UserExamId = "UE-002" }
            };
            var repo    = BuildRepo(items: items, total: 2);
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(
                new GetUserExamsQuery { UserId = "USER-001" },
                CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExams",
                TestCaseID        = "TC-GUEX-02",
                Description       = "2 exam sessions returned → Items.Count=2, TotalCount=2",
                ExpectedResult    = "IsSuccess=true, Items.Count=2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedHistoryAsync returns 2 items" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GUEX-03 | N | Paging metadata correct
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PagedRequest_ShouldReturnCorrectMetadata()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetPagedHistoryAsync(
                    "USER-001", null, null, 2, 5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<UserExamActionDto>(
                    new List<UserExamActionDto>(), 20, 2, 5));
            var handler = CreateHandler(repo);

            // Act
            var result = await handler.Handle(
                new GetUserExamsQuery { UserId = "USER-001", PageNumber = 2, PageSize = 5 },
                CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.PageNumber.Should().Be(2);
            result.Data.PageSize.Should().Be(5);
            result.Data.TotalCount.Should().Be(20);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExams",
                TestCaseID        = "TC-GUEX-03",
                Description       = "Page=2, Size=5 → paging metadata returned correctly",
                ExpectedResult    = "PageNumber=2, PageSize=5, TotalCount=20",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Paging params forwarded" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GUEX-04 | N | Filter by Status forwarded correctly
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterByStatus_ShouldForwardStatusToRepo()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetPagedHistoryAsync(
                    "USER-001", null, UserExamStatus.Completed,
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<UserExamActionDto>(
                    new List<UserExamActionDto>(), 0, 1, 10));
            var handler = CreateHandler(repo);

            // Act
            await handler.Handle(
                new GetUserExamsQuery { UserId = "USER-001", Status = UserExamStatus.Completed },
                CancellationToken.None);

            // Assert
            repo.Verify(x => x.GetPagedHistoryAsync(
                "USER-001", null, UserExamStatus.Completed,
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExams",
                TestCaseID        = "TC-GUEX-04",
                Description       = "Filter Status=Completed → repo called with Completed status",
                ExpectedResult    = "GetPagedHistoryAsync called with Status=Completed",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status filter forwarded" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GUEX-05 | N | Filter by ExamId forwarded correctly
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_FilterByExamId_ShouldForwardExamIdToRepo()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetPagedHistoryAsync(
                    "USER-001", "EXAM-001", null,
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<UserExamActionDto>(
                    new List<UserExamActionDto>(), 0, 1, 10));
            var handler = CreateHandler(repo);

            // Act
            await handler.Handle(
                new GetUserExamsQuery { UserId = "USER-001", ExamId = "EXAM-001" },
                CancellationToken.None);

            // Assert
            repo.Verify(x => x.GetPagedHistoryAsync(
                "USER-001", "EXAM-001", null,
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("UserExam - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExams",
                TestCaseID        = "TC-GUEX-05",
                Description       = "Filter by ExamId → repo called with correct ExamId",
                ExpectedResult    = "GetPagedHistoryAsync called with ExamId=EXAM-001",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ExamId filter forwarded" }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // TC-GUEX-06 | E | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IUserExamRepository>();
            repo.Setup(x => x.GetPagedHistoryAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<UserExamStatus?>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB timeout"));
            var handler = CreateHandler(repo);

            // Act
            var act = async () => await handler.Handle(
                new GetUserExamsQuery { UserId = "USER-001" }, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB timeout");

            // Excel Log
            QACollector.LogTestCase("UserExam - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetUserExams",
                TestCaseID        = "TC-GUEX-06",
                Description       = "Repository throws exception → propagates",
                ExpectedResult    = "Exception with 'DB timeout'",
                StatusRound1      = "Passed",
                TestCaseType      = "E",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedHistoryAsync throws Exception" }
            });
        }
    }
}
