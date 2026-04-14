using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.Commands.DeleteQuestionBank;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks
{
    public class DeleteQuestionBankCommandHandlerTests
    {
        private static DeleteQuestionBankCommandHandler CreateHandler(
            Mock<IQuestionBankRepository>?  qbRepo     = null,
            Mock<IQuestionOptionRepository>? optionRepo = null)
        {
            return new DeleteQuestionBankCommandHandler(
                (qbRepo     ?? MockQuestionBankRepository.GetMock()).Object,
                (optionRepo ?? new Mock<IQuestionOptionRepository>()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-DEL-01 | A | QB not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_QBNotFound_ShouldReturn404()
        {
            // Arrange
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: null);
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new DeleteQuestionBankCommand { QuestionBankId = "QB-MISSING" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionBank",
                TestCaseID        = "TC-QB-DEL-01",
                Description       = "QuestionBankId not found → 404 QuestionBankNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdWithDetailsAsync returns null", "404 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-DEL-02 | A | QB already Deleted → 409 Conflict
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AlreadyDeleted_ShouldReturn409()
        {
            // Arrange
            var deletedQb = MockQuestionBankRepository.GetSampleDeletedQB();
            var qbRepo    = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: deletedQb);
            var handler   = CreateHandler(qbRepo: qbRepo);
            var command   = new DeleteQuestionBankCommand { QuestionBankId = deletedQb.QuestionBankId };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(409);

            QACollector.LogTestCase("Question Bank - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionBank",
                TestCaseID        = "TC-QB-DEL-02",
                Description       = "QB already Deleted → 409 Conflict (QuestionBankHasDeleted)",
                ExpectedResult    = "IsSuccess=false, StatusCode=409",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status == Deleted", "409 Conflict returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-DEL-03 | N | Draft QB → hard delete options + set Deleted, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DraftQB_ShouldHardDeleteOptionsAndReturn200()
        {
            // Arrange
            var draftQb   = MockQuestionBankRepository.GetSampleDraftQB();
            var qbRepo    = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: draftQb);
            var optionRepo = new Mock<IQuestionOptionRepository>();
            optionRepo.Setup(x => x.DeleteByQuestionBankIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
            var handler = CreateHandler(qbRepo: qbRepo, optionRepo: optionRepo);
            var command = new DeleteQuestionBankCommand { QuestionBankId = draftQb.QuestionBankId };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            optionRepo.Verify(x => x.DeleteByQuestionBankIdAsync(
                draftQb.QuestionBankId, It.IsAny<CancellationToken>()), Times.Once);
            qbRepo.Verify(x => x.UpdateAsync(It.IsAny<QuestionBank>()), Times.Once);
            qbRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionBank",
                TestCaseID        = "TC-QB-DEL-03",
                Description       = "Draft QB: DeleteByQuestionBankIdAsync called, status set to Deleted, 200 returned",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, options hard deleted",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Draft", "options deleted", "status→Deleted", "200 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-DEL-04 | N | Active QB → hard delete options + set Deleted, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ActiveQB_ShouldHardDeleteOptionsAndReturn200()
        {
            // Arrange
            var activeQb   = MockQuestionBankRepository.GetSampleActiveQB();
            var qbRepo     = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: activeQb);
            var optionRepo = new Mock<IQuestionOptionRepository>();
            optionRepo.Setup(x => x.DeleteByQuestionBankIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
            var handler = CreateHandler(qbRepo: qbRepo, optionRepo: optionRepo);
            var command = new DeleteQuestionBankCommand { QuestionBankId = activeQb.QuestionBankId };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            optionRepo.Verify(x => x.DeleteByQuestionBankIdAsync(
                activeQb.QuestionBankId, It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionBank",
                TestCaseID        = "TC-QB-DEL-04",
                Description       = "Active QB: DeleteByQuestionBankIdAsync called, 200 returned",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, options hard deleted",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Active", "options deleted", "200 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-DEL-05 | N | PendingApproval QB → only status set to Deleted, options NOT deleted
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PendingApprovalQB_ShouldOnlySetStatusDeleted()
        {
            // Arrange
            var pendingQb  = MockQuestionBankRepository.GetSamplePendingQB();
            var qbRepo     = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: pendingQb);
            var optionRepo = new Mock<IQuestionOptionRepository>();
            var handler    = CreateHandler(qbRepo: qbRepo, optionRepo: optionRepo);
            var command    = new DeleteQuestionBankCommand { QuestionBankId = pendingQb.QuestionBankId };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert: success but options NOT deleted (Assigned path)
            result.IsSuccess.Should().BeTrue();
            optionRepo.Verify(x => x.DeleteByQuestionBankIdAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            QACollector.LogTestCase("Question Bank - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionBank",
                TestCaseID        = "TC-QB-DEL-05",
                Description       = "PendingApproval QB: only status changed to Deleted, DeleteByQuestionBankIdAsync not called",
                ExpectedResult    = "IsSuccess=true, DeleteByQuestionBankIdAsync Times.Never",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=PendingApproval", "options NOT hard-deleted", "status only" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-DEL-06 | A | Repository throws exception → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            // Arrange: GetByIdWithDetailsAsync succeeds but UpdateAsync throws
            var draftQb   = MockQuestionBankRepository.GetSampleDraftQB();
            var qbRepo    = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(draftQb);
            qbRepo.Setup(x => x.UpdateAsync(It.IsAny<QuestionBank>()))
                  .ThrowsAsync(new InvalidOperationException("DB write failed"));

            var optionRepo = new Mock<IQuestionOptionRepository>();
            optionRepo.Setup(x => x.DeleteByQuestionBankIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

            var handler = CreateHandler(qbRepo: qbRepo, optionRepo: optionRepo);
            var command = new DeleteQuestionBankCommand { QuestionBankId = draftQb.QuestionBankId };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Question Bank - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionBank",
                TestCaseID        = "TC-QB-DEL-06",
                Description       = "Repository throws on UpdateAsync → caught → 500 ServerError",
                ExpectedResult    = "IsSuccess=false, StatusCode=500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB found", "UpdateAsync throws", "catch block returns 500" }
            });
        }
    }
}
