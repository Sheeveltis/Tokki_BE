using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionTypes.Queries.GetQuestionTypes;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionTypes
{
    public class GetQuestionTypesQueryHandlerTests
    {
        private static GetQuestionTypesQueryHandler CreateHandler(
            Mock<IQuestionTypeRepository>? repo = null)
        {
            return new GetQuestionTypesQueryHandler(
                (repo ?? MockQuestionTypeRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionTypes_01 | N | Empty repository → empty paged result, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyRepository_ShouldReturnEmptyPagedResult()
        {
            // Arrange - paged returns empty
            var repo    = MockQuestionTypeRepository.GetMock(pagedResult: (new List<QuestionType>(), 0));
            var handler = CreateHandler(repo);
            var query   = new GetQuestionTypesQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Question Type - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionTypes",
                TestCaseID        = "GetQuestionTypes_01",
                Description       = "Empty repository → PagedResult.Items empty, TotalCount=0, success",
                ExpectedResult    = "IsSuccess=true, Items empty, TotalCount=0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedAsync returns ([], 0)", "empty paged result" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionTypes_02 | N | 3 items → paged result with correct count
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ThreeItems_ShouldReturnPagedResultWithCorrectCount()
        {
            // Arrange
            var items   = MockQuestionTypeRepository.GetSampleList(3);
            var repo    = MockQuestionTypeRepository.GetMock(pagedResult: (items, 3));
            var handler = CreateHandler(repo);
            var query   = new GetQuestionTypesQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(3);
            result.Data.TotalCount.Should().Be(3);

            QACollector.LogTestCase("Question Type - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionTypes",
                TestCaseID        = "GetQuestionTypes_02",
                Description       = "3 items from repo → PagedResult.Items.Count=3, TotalCount=3",
                ExpectedResult    = "IsSuccess=true, Items.Count=3, TotalCount=3",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedAsync returns 3 items", "correct count" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionTypes_03 | N | Paging: Page 2 with 5 items → correct PageNumber reflected
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Page2_ShouldReturnPageNumber2InResult()
        {
            // Arrange
            var items   = MockQuestionTypeRepository.GetSampleList(2);
            var repo    = MockQuestionTypeRepository.GetMock(pagedResult: (items, 12));
            var handler = CreateHandler(repo);
            var query   = new GetQuestionTypesQuery { PageNumber = 2, PageSize = 5 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.PageNumber.Should().Be(2);
            result.Data.PageSize.Should().Be(5);
            result.Data.TotalCount.Should().Be(12);

            QACollector.LogTestCase("Question Type - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionTypes",
                TestCaseID        = "GetQuestionTypes_03",
                Description       = "Page 2, size 5, totalCount 12 → PagedResult reflects all pagination params",
                ExpectedResult    = "IsSuccess=true, PageNumber=2, PageSize=5, TotalCount=12",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PageNumber=2, PageSize=5", "totalCount=12" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionTypes_04 | B | GetPagedAsync called with correct filter params
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WithFilters_GetPagedCalledWithCorrectParams()
        {
            // Arrange
            var repo    = MockQuestionTypeRepository.GetMock(pagedResult: (new List<QuestionType>(), 0));
            var handler = CreateHandler(repo);
            var query   = new GetQuestionTypesQuery
            {
                PageNumber = 1,
                PageSize   = 20,
                Keyword    = "Reading",
                Skill      = QuestionSkill.Reading,
                Difficulty = DifficultyLevel.Medium,
                ExamType   = ExamType.TopikI
            };

            // Act
            await handler.Handle(query, CancellationToken.None);

            // Assert
            repo.Verify(x => x.GetPagedAsync(
                1,
                20,
                "Reading",
                QuestionSkill.Reading,
                DifficultyLevel.Medium,
                ExamType.TopikI,
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Type - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionTypes",
                TestCaseID        = "GetQuestionTypes_04",
                Description       = "Boundary: GetPagedAsync called with exact filter params (keyword, skill, difficulty, examType)",
                ExpectedResult    = "GetPagedAsync(1, 20, 'Reading', Reading, Medium, TOEIC) Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "All filters set in query", "passed through to repo" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionTypes_05 | B | TotalPages calculated correctly
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PaginationMath_ShouldCalculateTotalPagesCorrectly()
        {
            // Arrange — 11 items, page size 5 → TotalPages = ceil(11/5) = 3
            var items   = MockQuestionTypeRepository.GetSampleList(5);
            var repo    = MockQuestionTypeRepository.GetMock(pagedResult: (items, 11));
            var handler = CreateHandler(repo);
            var query   = new GetQuestionTypesQuery { PageNumber = 1, PageSize = 5 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Data!.TotalCount.Should().Be(11);
            result.Data.TotalPages.Should().Be(3); // ceil(11/5) = 3

            QACollector.LogTestCase("Question Type - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionTypes",
                TestCaseID        = "GetQuestionTypes_05",
                Description       = "Boundary: TotalPages = ceil(11/5) = 3",
                ExpectedResult    = "TotalPages=3 (11 items, pageSize=5)",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TotalCount=11, PageSize=5", "TotalPages=3" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionTypes_06 | A | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var repo = new Mock<IQuestionTypeRepository>();
            repo.Setup(x => x.GetPagedAsync(
                        It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<string?>(), It.IsAny<QuestionSkill?>(),
                        It.IsAny<DifficultyLevel?>(), It.IsAny<ExamType?>(),
                        It.IsAny<bool?>(),
                        It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("DB read error"));
            var handler = CreateHandler(repo);
            var query   = new GetQuestionTypesQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var act = async () => await handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB read error");

            QACollector.LogTestCase("Question Type - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionTypes",
                TestCaseID        = "GetQuestionTypes_06",
                Description       = "Repository GetPagedAsync throws → exception propagates (no catch in handler)",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedAsync throws", "no catch block" }
            });
        }
    }
}
