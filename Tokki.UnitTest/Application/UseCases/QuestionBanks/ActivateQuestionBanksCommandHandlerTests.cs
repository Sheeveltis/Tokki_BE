using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.Commands.ActivateQuestionBanks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks
{
    public class ActivateQuestionBanksCommandHandlerTests
    {
        private static ActivateQuestionBanksCommandHandler CreateHandler(
            Mock<IQuestionBankRepository>? qbRepo = null)
        {
            var logger = new Mock<ILogger<ActivateQuestionBanksCommandHandler>>();
            return new ActivateQuestionBanksCommandHandler(
                (qbRepo ?? MockQuestionBankRepository.GetMock()).Object,
                logger.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-ACT-01 | A | Empty QuestionBankIds → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyIds_ShouldReturn400()
        {
            // Arrange
            var handler = CreateHandler();
            var command = new ActivateQuestionBanksCommand { QuestionBankIds = new List<string>() };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Activate", new TestCaseDetail
            {
                FunctionGroup     = "ActivateQuestionBanks",
                TestCaseID        = "TC-QB-ACT-01",
                Description       = "Empty QuestionBankIds list → 400 BadRequest",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ids.Count == 0", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-ACT-02 | A | One ID not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_IdNotFound_ShouldReturn404()
        {
            // Arrange — GetByIdsAsync returns empty
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIds: new List<QuestionBank>());
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new ActivateQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-MISSING" }
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Activate", new TestCaseDetail
            {
                FunctionGroup     = "ActivateQuestionBanks",
                TestCaseID        = "TC-QB-ACT-02",
                Description       = "Requested QB not found in DB → 404 QuestionBankNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdsAsync returns empty", "notFound.Count > 0", "404 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-ACT-03 | A | Non-Draft QB in list → 403 Forbidden
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NonDraftQB_ShouldReturn403()
        {
            // Arrange — QB is PendingApproval (not Draft)
            var pendingQb = MockQuestionBankRepository.GetSamplePendingQB();
            var qbRepo    = MockQuestionBankRepository.GetMock(
                returnedByIds: new List<QuestionBank> { pendingQb });
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new ActivateQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { pendingQb.QuestionBankId }
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Question Bank - Activate", new TestCaseDetail
            {
                FunctionGroup     = "ActivateQuestionBanks",
                TestCaseID        = "TC-QB-ACT-03",
                Description       = "QB not in Draft status (PendingApproval) → 403 Forbidden",
                ExpectedResult    = "IsSuccess=false, StatusCode=403",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=PendingApproval", "only Draft allowed", "403 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-ACT-04 | N | Happy path: Draft QBs → all set Active, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DraftQBs_ShouldActivateAndReturn200()
        {
            // Arrange
            var draft1  = MockQuestionBankRepository.GetSampleDraftQB("QB-DRAFT-01");
            var draft2  = MockQuestionBankRepository.GetSampleDraftQB("QB-DRAFT-02");
            var qbRepo  = MockQuestionBankRepository.GetMock(
                returnedByIds: new List<QuestionBank> { draft1, draft2 });
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new ActivateQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-DRAFT-01", "QB-DRAFT-02" }
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(2);
            qbRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<IEnumerable<QuestionBank>>()), Times.Once);
            qbRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Activate", new TestCaseDetail
            {
                FunctionGroup     = "ActivateQuestionBanks",
                TestCaseID        = "TC-QB-ACT-04",
                Description       = "2 Draft QBs → activated, Data=2, UpdateRangeAsync+SaveChanges called, 200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, Data=2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "All QB.Status=Draft", "UpdateRangeAsync once", "200 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-ACT-05 | N | Data returned equals count of activated QBs
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DraftQBs_ShouldReturnCorrectActivatedCount()
        {
            // Arrange
            var drafts = new List<QuestionBank>
            {
                MockQuestionBankRepository.GetSampleDraftQB("QB-D-01"),
                MockQuestionBankRepository.GetSampleDraftQB("QB-D-02"),
                MockQuestionBankRepository.GetSampleDraftQB("QB-D-03")
            };
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIds: drafts);
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new ActivateQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-D-01", "QB-D-02", "QB-D-03" }
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(3);

            QACollector.LogTestCase("Question Bank - Activate", new TestCaseDetail
            {
                FunctionGroup     = "ActivateQuestionBanks",
                TestCaseID        = "TC-QB-ACT-05",
                Description       = "3 Draft QBs activated → Data (activated count) = 3",
                ExpectedResult    = "IsSuccess=true, Data=3",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "3 Draft QBs", "items.Count=3 returned as Data" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-ACT-06 | A | Repository throws on UpdateRangeAsync → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            // Arrange
            var draftQb = MockQuestionBankRepository.GetSampleDraftQB();
            var qbRepo  = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<QuestionBank> { draftQb });
            qbRepo.Setup(x => x.UpdateRangeAsync(It.IsAny<IEnumerable<QuestionBank>>()))
                  .ThrowsAsync(new InvalidOperationException("DB error"));

            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new ActivateQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { draftQb.QuestionBankId }
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Question Bank - Activate", new TestCaseDetail
            {
                FunctionGroup     = "ActivateQuestionBanks",
                TestCaseID        = "TC-QB-ACT-06",
                Description       = "UpdateRangeAsync throws exception → caught → 500 ServerError",
                ExpectedResult    = "IsSuccess=false, StatusCode=500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Draft QB found", "UpdateRangeAsync throws", "500 returned" }
            });
        }
    }
}
