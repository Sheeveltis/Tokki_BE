using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBanks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks
{
    public class GetQuestionBanksQueryHandlerTests
    {
        private static GetQuestionBanksQueryHandler CreateHandler(
            Mock<IQuestionBankRepository>? qbRepo = null)
        {
            return new GetQuestionBanksQueryHandler(
                (qbRepo ?? MockQuestionBankRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionBanks_01 | N | Happy path with items → PagedResult, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_HasItems_ShouldReturn200WithPagedResult()
        {
            // Arrange
            var qbs    = MockQuestionBankRepository.GetSampleQBList();
            var qbRepo = MockQuestionBankRepository.GetMock(
                pagedResult: (qbs, qbs.Count));
            var handler = CreateHandler(qbRepo);
            var query   = new GetQuestionBanksQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(3);

            QACollector.LogTestCase("Question Bank - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBanks",
                TestCaseID        = "GetQuestionBanks_01",
                Description       = "Happy path: 3 QBs in DB → PagedResult with 3 items, 200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, Items.Count=3",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedAsync returns 3 items", "200 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionBanks_02 | N | Empty result → PagedResult with 0 items
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoItems_ShouldReturnEmptyPagedResult()
        {
            // Arrange
            var qbRepo = MockQuestionBankRepository.GetMock(
                pagedResult: (new List<QuestionBank>(), 0));
            var handler = CreateHandler(qbRepo);
            var query   = new GetQuestionBanksQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data!.TotalCount.Should().Be(0);

            QACollector.LogTestCase("Question Bank - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBanks",
                TestCaseID        = "GetQuestionBanks_02",
                Description       = "No QBs in DB → empty PagedResult, IsSuccess=true",
                ExpectedResult    = "IsSuccess=true, Items=[], TotalCount=0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedAsync returns ([], 0)", "empty result" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionBanks_03 | N | PagedResult metadata correct
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_HasItems_ShouldSetPagedMetadataCorrectly()
        {
            // Arrange
            var qbs    = MockQuestionBankRepository.GetSampleQBList();
            var qbRepo = MockQuestionBankRepository.GetMock(pagedResult: (qbs, 100));
            var handler = CreateHandler(qbRepo);
            var query   = new GetQuestionBanksQuery { PageNumber = 3, PageSize = 5 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Data!.TotalCount.Should().Be(100);
            result.Data!.PageNumber.Should().Be(3);
            result.Data!.PageSize.Should().Be(5);

            QACollector.LogTestCase("Question Bank - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBanks",
                TestCaseID        = "GetQuestionBanks_03",
                Description       = "PagedResult metadata (TotalCount=100, PageNumber=3, PageSize=5) correctly set",
                ExpectedResult    = "TotalCount=100, PageNumber=3, PageSize=5",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PageNumber=3, PageSize=5", "TotalCount from repo=100" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionBanks_04 | N | CreateBy filter applied after paging
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CreateByFilter_ShouldFilterResultsInHandler()
        {
            // Arrange — two QBs with different creators
            var qb1 = MockQuestionBankRepository.GetSampleActiveQB("QB-C1");
            qb1.CreateBy = "STAFF-001";
            var qb2 = MockQuestionBankRepository.GetSampleActiveQB("QB-C2");
            qb2.CreateBy = "STAFF-002";
            var qbRepo = MockQuestionBankRepository.GetMock(pagedResult: (new List<QuestionBank> { qb1, qb2 }, 2));
            var handler = CreateHandler(qbRepo);
            var query = new GetQuestionBanksQuery
            {
                PageNumber = 1,
                PageSize   = 10,
                CreateBy   = "STAFF-001"
            };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert: only QB created by STAFF-001
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            result.Data!.Items[0].CreateBy.Should().Be("STAFF-001");

            QACollector.LogTestCase("Question Bank - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBanks",
                TestCaseID        = "GetQuestionBanks_04",
                Description       = "CreateBy filter applied after paging → only STAFF-001 QBs returned",
                ExpectedResult    = "Items.Count=1, Items[0].CreateBy='STAFF-001'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "2 QBs from repo", "CreateBy='STAFF-001' filter", "1 item returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionBanks_05 | A | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetPagedAsync(
                        It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<string?>(), It.IsAny<string?>(),
                        It.IsAny<string?>(), It.IsAny<QuestionBankStatus?>(),
                        It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("DB timeout"));
            var handler = CreateHandler(qbRepo);

            // Act
            var act = async () => await handler.Handle(
                new GetQuestionBanksQuery(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB timeout");

            QACollector.LogTestCase("Question Bank - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBanks",
                TestCaseID        = "GetQuestionBanks_05",
                Description       = "Repository throws exception on GetPagedAsync → propagates to caller",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPagedAsync throws exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // GetQuestionBanks_06 | B | GetPagedAsync called with correct pagination params
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidQuery_ShouldCallGetPagedAsyncWithCorrectParams()
        {
            // Arrange
            var qbRepo  = MockQuestionBankRepository.GetMock(pagedResult: (new List<QuestionBank>(), 0));
            var handler = CreateHandler(qbRepo);
            var query   = new GetQuestionBanksQuery
            {
                PageNumber     = 2,
                PageSize       = 15,
                SearchTerm     = "grammar",
                QuestionTypeId = "QT-001",
                Status         = QuestionBankStatus.Active
            };

            // Act
            await handler.Handle(query, CancellationToken.None);

            // Assert: paging params forwarded verbatim
            qbRepo.Verify(x => x.GetPagedAsync(
                2, 15, "grammar", "QT-001", null, QuestionBankStatus.Active,
                It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Get List", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBanks",
                TestCaseID        = "GetQuestionBanks_06",
                Description       = "Boundary: GetPagedAsync called with exact query params (PageNumber=2, PageSize=15, searchTerm, typeId, status)",
                ExpectedResult    = "GetPagedAsync(2,15,'grammar','QT-001',null,Active,...) Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Full query params provided", "Params forwarded verbatim" }
            });
        }
    }
}
