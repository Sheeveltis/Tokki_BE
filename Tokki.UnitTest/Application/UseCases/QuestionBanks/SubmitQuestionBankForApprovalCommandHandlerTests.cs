using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.Commands.SubmitQuestionBankForApproval;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks
{
    public class SubmitQuestionBankForApprovalCommandHandlerTests
    {
        private static SubmitQuestionBankForApprovalCommandHandler CreateHandler(
            Mock<IQuestionBankRepository>? qbRepo = null)
        {
            return new SubmitQuestionBankForApprovalCommandHandler(
                (qbRepo ?? MockQuestionBankRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-SUB-01 | A | Empty QuestionBankIds → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyIds_ShouldReturn400()
        {
            // Arrange
            var handler = CreateHandler();
            var command = new SubmitQuestionBankForApprovalCommand
            {
                QuestionBankIds = new List<string>()
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "SubmitQuestionBankForApproval",
                TestCaseID        = "TC-QB-SUB-01",
                Description       = "Empty QuestionBankIds → 400 ValidationFailed",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ids.Count == 0", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-SUB-02 | A | QB ID not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_IdNotFound_ShouldReturn404()
        {
            // Arrange
            var qbRepo = MockQuestionBankRepository.GetMock(returnedById: null);
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new SubmitQuestionBankForApprovalCommand
            {
                QuestionBankIds = new List<string> { "QB-MISSING" }
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "SubmitQuestionBankForApproval",
                TestCaseID        = "TC-QB-SUB-02",
                Description       = "QuestionBankId not found → 404 QuestionBankNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "404 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-SUB-03 | A | QB not in Draft or Rejected → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ActiveQB_ShouldReturn400()
        {
            // Arrange — Active QB cannot be submitted
            var activeQb = MockQuestionBankRepository.GetSampleActiveQB();
            var qbRepo   = MockQuestionBankRepository.GetMock(returnedById: activeQb);
            var handler  = CreateHandler(qbRepo: qbRepo);
            var command  = new SubmitQuestionBankForApprovalCommand
            {
                QuestionBankIds = new List<string> { activeQb.QuestionBankId }
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "SubmitQuestionBankForApproval",
                TestCaseID        = "TC-QB-SUB-03",
                Description       = "Active QB cannot be submitted (not Draft/Rejected) → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Active", "not Draft/Rejected", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-SUB-04 | N | Draft QB → submitted to PendingApproval, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DraftQB_ShouldSubmitAndReturn200()
        {
            // Arrange
            var draftQb = MockQuestionBankRepository.GetSampleDraftQB();
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedById: draftQb);
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new SubmitQuestionBankForApprovalCommand
            {
                QuestionBankIds = new List<string> { draftQb.QuestionBankId }
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Contain(draftQb.QuestionBankId);
            qbRepo.Verify(x => x.UpdateAsync(It.IsAny<QuestionBank>()), Times.Once);
            qbRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "SubmitQuestionBankForApproval",
                TestCaseID        = "TC-QB-SUB-04",
                Description       = "Draft QB → submitted, status=PendingApproval, UpdateAsync+SaveChanges called, 200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, Data contains QB id",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Draft", "UpdateAsync once, SaveChanges once", "200 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-SUB-05 | N | Rejected QB → re-submitted to PendingApproval, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RejectedQB_ShouldResubmitAndReturn200()
        {
            // Arrange
            var rejectedQb = MockQuestionBankRepository.GetSampleRejectedQB();
            var qbRepo     = MockQuestionBankRepository.GetMock(returnedById: rejectedQb);
            var handler    = CreateHandler(qbRepo: qbRepo);
            var command    = new SubmitQuestionBankForApprovalCommand
            {
                QuestionBankIds = new List<string> { rejectedQb.QuestionBankId }
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Contain(rejectedQb.QuestionBankId);

            QACollector.LogTestCase("Question Bank - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "SubmitQuestionBankForApproval",
                TestCaseID        = "TC-QB-SUB-05",
                Description       = "Rejected QB → re-submitted to PendingApproval, ApprovedBy reset, 200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, Data contains QB id",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Rejected", "status→PendingApproval", "audit fields reset" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-SUB-06 | N | ApprovedBy and ApprovedDate reset on resubmit
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RejectedQB_ShouldResetAuditFields()
        {
            // Arrange
            var rejectedQb = MockQuestionBankRepository.GetSampleRejectedQB();
            rejectedQb.ApprovedBy   = "ADMIN-001";
            rejectedQb.ApprovedDate = DateTime.UtcNow.AddDays(-1);
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedById: rejectedQb);
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new SubmitQuestionBankForApprovalCommand
            {
                QuestionBankIds = new List<string> { rejectedQb.QuestionBankId }
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert: verify UpdateAsync was called with entity having reset audit
            qbRepo.Verify(x => x.UpdateAsync(It.Is<QuestionBank>(
                qb => qb.ApprovedBy == null && qb.ApprovedDate == null)), Times.Once);

            QACollector.LogTestCase("Question Bank - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "SubmitQuestionBankForApproval",
                TestCaseID        = "TC-QB-SUB-06",
                Description       = "Resubmit resets ApprovedBy and ApprovedDate to null",
                ExpectedResult    = "UpdateAsync called with ApprovedBy=null, ApprovedDate=null",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB previously had ApprovedBy set", "after submit: both null" }
            });
        }
    }
}
