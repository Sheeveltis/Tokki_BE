using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetByQuestionTypeId;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks
{
    public class GetQuestionBanksByQuestionTypeIdQueryHandlerTests
    {
        private static GetQuestionBanksByQuestionTypeIdQueryHandler CreateHandler(
            Mock<IQuestionBankRepository>? qbRepo = null)
        {
            return new GetQuestionBanksByQuestionTypeIdQueryHandler(
                (qbRepo ?? MockQuestionBankRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GBT-01 | A | Empty QuestionTypeId → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyQuestionTypeId_ShouldReturn400()
        {
            // Arrange
            var handler = CreateHandler();
            var query   = new GetQuestionBanksByQuestionTypeIdQuery { QuestionTypeId = "" };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Get By TypeId", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBanksByQuestionTypeId",
                TestCaseID        = "TC-QB-GBT-01",
                Description       = "Empty QuestionTypeId → 400 ValidationFailed",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionTypeId = empty string", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GBT-02 | N | Happy path → list of DTOs returned, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidTypeId_ShouldReturn200WithList()
        {
            // Arrange
            var qbs    = MockQuestionBankRepository.GetSampleQBList();
            var qbRepo = MockQuestionBankRepository.GetMock(returnedByTypeId: qbs);
            var handler = CreateHandler(qbRepo);
            var query   = new GetQuestionBanksByQuestionTypeIdQuery { QuestionTypeId = "QT-001" };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.Count.Should().Be(3);

            QACollector.LogTestCase("Question Bank - Get By TypeId", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBanksByQuestionTypeId",
                TestCaseID        = "TC-QB-GBT-02",
                Description       = "Happy path: valid QuestionTypeId → 200 with mapped DTO list",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, Data.Count=3",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByQuestionTypeIdAsync returns 3 items", "200 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GBT-03 | N | No QBs for type → empty list, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoQBsForType_ShouldReturnEmptyList()
        {
            // Arrange
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByTypeId: new List<QuestionBank>());
            var handler = CreateHandler(qbRepo);
            var query   = new GetQuestionBanksByQuestionTypeIdQuery { QuestionTypeId = "QT-EMPTY" };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("Question Bank - Get By TypeId", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBanksByQuestionTypeId",
                TestCaseID        = "TC-QB-GBT-03",
                Description       = "No QBs for given QuestionTypeId → empty list with IsSuccess=true",
                ExpectedResult    = "IsSuccess=true, Data=[]",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repo returns empty list", "result maps to empty DTO list" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GBT-04 | N | CreateBy filter applied in handler
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CreateByFilter_ShouldFilterResultsInHandler()
        {
            // Arrange
            var qb1 = MockQuestionBankRepository.GetSampleActiveQB("QB-C1"); qb1.CreateBy = "STAFF-001";
            var qb2 = MockQuestionBankRepository.GetSampleActiveQB("QB-C2"); qb2.CreateBy = "STAFF-002";
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByTypeId: new List<QuestionBank> { qb1, qb2 });
            var handler = CreateHandler(qbRepo);
            var query   = new GetQuestionBanksByQuestionTypeIdQuery
            {
                QuestionTypeId = "QT-001",
                CreateBy       = "STAFF-001"
            };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Count.Should().Be(1);
            result.Data![0].CreateBy.Should().Be("STAFF-001");

            QACollector.LogTestCase("Question Bank - Get By TypeId", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBanksByQuestionTypeId",
                TestCaseID        = "TC-QB-GBT-04",
                Description       = "CreateBy filter applied in handler → only STAFF-001 QBs returned",
                ExpectedResult    = "Data.Count=1, Data[0].CreateBy='STAFF-001'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "2 QBs from repo", "CreateBy='STAFF-001'", "1 item returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GBT-05 | A | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetByQuestionTypeIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<QuestionBankStatus?>(),
                        It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("DB error"));
            var handler = CreateHandler(qbRepo);
            var query   = new GetQuestionBanksByQuestionTypeIdQuery { QuestionTypeId = "QT-001" };

            // Act
            var act = async () => await handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB error");

            QACollector.LogTestCase("Question Bank - Get By TypeId", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBanksByQuestionTypeId",
                TestCaseID        = "TC-QB-GBT-05",
                Description       = "Repository throws exception → propagates to caller",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByQuestionTypeIdAsync throws" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GBT-06 | B | QuestionTypeId trimmed before repo call
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_QuestionTypeIdWithSpaces_ShouldTrimBeforeRepoCall()
        {
            // Arrange
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByTypeId: new List<QuestionBank>());
            var handler = CreateHandler(qbRepo);
            var query   = new GetQuestionBanksByQuestionTypeIdQuery { QuestionTypeId = "  QT-001  " };

            // Act
            await handler.Handle(query, CancellationToken.None);

            // Assert: repo called with trimmed type id
            qbRepo.Verify(x => x.GetByQuestionTypeIdAsync(
                "QT-001",
                It.IsAny<QuestionBankStatus?>(),
                It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Get By TypeId", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBanksByQuestionTypeId",
                TestCaseID        = "TC-QB-GBT-06",
                Description       = "Boundary: QuestionTypeId with spaces → trimmed before GetByQuestionTypeIdAsync call",
                ExpectedResult    = "GetByQuestionTypeIdAsync('QT-001',...) called Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionTypeId='  QT-001  '", "Trim() applied", "repo called with 'QT-001'" }
            });
        }
    }
}
