using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBankById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks
{
    public class GetQuestionBankByIdQueryHandlerTests
    {
        private static GetQuestionBankByIdQueryHandler CreateHandler(
            Mock<IQuestionBankRepository>? qbRepo = null)
        {
            return new GetQuestionBankByIdQueryHandler(
                (qbRepo ?? MockQuestionBankRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GBI-01 | A | QB not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_QBNotFound_ShouldReturn404()
        {
            // Arrange
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: null);
            var handler = CreateHandler(qbRepo);
            var query   = new GetQuestionBankByIdQuery { QuestionBankId = "QB-MISSING" };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBankById",
                TestCaseID        = "TC-QB-GBI-01",
                Description       = "QuestionBankId not found → 404 QuestionBankNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdWithDetailsAsync returns null", "404 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GBI-02 | N | Happy path → DTO correctly mapped, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidQB_ShouldReturn200WithMappedDTO()
        {
            // Arrange
            var qb = MockQuestionBankRepository.GetSampleQBWithOptions();
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: qb);
            var handler = CreateHandler(qbRepo);
            var query   = new GetQuestionBankByIdQuery { QuestionBankId = qb.QuestionBankId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.QuestionBankId.Should().Be(qb.QuestionBankId);
            result.Data!.Content.Should().Be(qb.Content);
            result.Data!.Status.Should().Be(qb.Status);
            result.Data!.Options.Should().HaveCount(2);

            QACollector.LogTestCase("Question Bank - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBankById",
                TestCaseID        = "TC-QB-GBI-02",
                Description       = "Happy path: QB found with options → QuestionBankDto correctly mapped, 200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, Data.Options.Count=2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB found", "QBDTO fully mapped", "Options list mapped" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GBI-03 | N | Options sorted by KeyOption ascending
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidQB_OptionsSortedByKeyOption()
        {
            // Arrange: options added in reverse order
            var qb = new QuestionBank
            {
                QuestionBankId  = "QB-SORT-01",
                Status          = QuestionBankStatus.Active,
                Content         = "Sort test",
                QuestionOptions = new List<QuestionOption>
                {
                    new QuestionOption { OptionId = "OPT-B", KeyOption = "B", Content = "Option B", IsCorrect = false },
                    new QuestionOption { OptionId = "OPT-A", KeyOption = "A", Content = "Option A", IsCorrect = true  }
                }
            };
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: qb);
            var handler = CreateHandler(qbRepo);

            // Act
            var result = await handler.Handle(
                new GetQuestionBankByIdQuery { QuestionBankId = "QB-SORT-01" }, CancellationToken.None);

            // Assert: first option should be A
            result.IsSuccess.Should().BeTrue();
            result.Data!.Options[0].KeyOption.Should().Be("A");
            result.Data!.Options[1].KeyOption.Should().Be("B");

            QACollector.LogTestCase("Question Bank - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBankById",
                TestCaseID        = "TC-QB-GBI-03",
                Description       = "Options are sorted by KeyOption ascending in the mapped DTO",
                ExpectedResult    = "Options[0].KeyOption='A', Options[1].KeyOption='B'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Options added B then A", "DTO sorts by KeyOption" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GBI-04 | N | Nav props null → DTO PassageTitle/QuestionTypeName null
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoNavProps_ShouldMapNullForNavFields()
        {
            // Arrange
            var qb = new QuestionBank
            {
                QuestionBankId  = "QB-NAV-01",
                Status          = QuestionBankStatus.Active,
                Passage         = null,
                QuestionType    = null,
                QuestionOptions = new List<QuestionOption>()
            };
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: qb);
            var handler = CreateHandler(qbRepo);

            // Act
            var result = await handler.Handle(
                new GetQuestionBankByIdQuery { QuestionBankId = "QB-NAV-01" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.PassageTitle.Should().BeNull();
            result.Data!.QuestionTypeName.Should().BeNull();

            QACollector.LogTestCase("Question Bank - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBankById",
                TestCaseID        = "TC-QB-GBI-04",
                Description       = "Navigation props (Passage, QuestionType) null → PassageTitle/QuestionTypeName null in DTO",
                ExpectedResult    = "PassageTitle=null, QuestionTypeName=null",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "entity.Passage = null", "entity.QuestionType = null", "DTO uses ?." }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GBI-05 | A | Repository throws → exception propagates
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("DB error"));
            var handler = CreateHandler(qbRepo);

            // Act
            var act = async () => await handler.Handle(
                new GetQuestionBankByIdQuery { QuestionBankId = "QB-001" }, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB error");

            QACollector.LogTestCase("Question Bank - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBankById",
                TestCaseID        = "TC-QB-GBI-05",
                Description       = "Repository throws exception → propagates to caller",
                ExpectedResult    = "InvalidOperationException thrown",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdWithDetailsAsync throws" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-GBI-06 | B | GetByIdWithDetailsAsync called with exact QB id
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidQuery_ShouldCallGetByIdWithDetailsWithCorrectId()
        {
            // Arrange
            const string qbId = "QB-SPECIFIC-01";
            var qb     = MockQuestionBankRepository.GetSampleActiveQB(qbId);
            var qbRepo = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: qb);
            var handler = CreateHandler(qbRepo);

            // Act
            await handler.Handle(
                new GetQuestionBankByIdQuery { QuestionBankId = qbId }, CancellationToken.None);

            // Assert
            qbRepo.Verify(x => x.GetByIdWithDetailsAsync(qbId, It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionBankById",
                TestCaseID        = "TC-QB-GBI-06",
                Description       = "Boundary: GetByIdWithDetailsAsync called exactly once with the exact QuestionBankId",
                ExpectedResult    = "GetByIdWithDetailsAsync('QB-SPECIFIC-01') Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Specific QB id in query", "repo called once" }
            });
        }
    }
}
