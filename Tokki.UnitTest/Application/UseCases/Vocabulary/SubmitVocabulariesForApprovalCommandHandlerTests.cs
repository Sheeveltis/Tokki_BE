using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.Commands.SubmitVocabulariesForApproval;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class SubmitVocabulariesForApprovalCommandHandlerTests
    {
        private SubmitVocabulariesForApprovalCommandHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            bool unauthorized = false)
        {
            return new SubmitVocabulariesForApprovalCommandHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                MockVocabularyExampleRepository.GetMock().Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("STAFF-001").Object,
                new Mock<ILogger<SubmitVocabulariesForApprovalCommandHandler>>().Object);
        }

        // ═══════════════════════════════════════════════════════════
        // Submit_Vocabularies_For_Approval_01 | A | No token → 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            // Arrange
            var command = new SubmitVocabulariesForApprovalCommand
            {
                VocabularyIds = new List<string> { "VOCAB-003" }
            };
            var handler = CreateHandler(unauthorized: true);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "Submit Vocabularies For Approval",
                TestCaseID        = "Submit_Vocabularies_For_Approval_01",
                Description       = "Submit vocabulary for approval without authentication token",
                ExpectedResult    = "Return 401 Unauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No UserId in Claims", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Submit_Vocabularies_For_Approval_02 | A | Empty VocabularyIds → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyVocabularyIds_ShouldReturn400()
        {
            // Arrange
            var command = new SubmitVocabulariesForApprovalCommand { VocabularyIds = new List<string>() };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "Submit Vocabularies For Approval",
                TestCaseID        = "Submit_Vocabularies_For_Approval_02",
                Description       = "Submit with an empty vocabulary list",
                ExpectedResult    = "Return 400 VOCABULARY_EMPTY",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyIds = empty", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Submit_Vocabularies_For_Approval_03 | A | Vocab not found → exception → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabNotFound_ShouldReturn500()
        {
            // Arrange
            var command = new SubmitVocabulariesForApprovalCommand
            {
                VocabularyIds = new List<string> { "VOCAB-INVALID" }
            };
            var mockVocabRepo = MockVocabularyRepository.GetMock(returnedVocab: null);
            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "Submit Vocabularies For Approval",
                TestCaseID        = "Submit_Vocabularies_For_Approval_03",
                Description       = "Submit vocab that doesn't exist → exception → rollback → 500",
                ExpectedResult    = "Transaction rollback, return 500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Vocab = null", "Exception thrown", "Return 500" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Submit_Vocabularies_For_Approval_04 | A | Vocab not Draft (Active) → exception → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabNotDraft_ShouldReturn500()
        {
            // Arrange
            var command = new SubmitVocabulariesForApprovalCommand
            {
                VocabularyIds = new List<string> { "VOCAB-001" }
            };
            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedVocab: MockVocabularyRepository.GetSampleVocabulary(status: VocabularyStatus.Active));
            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "Submit Vocabularies For Approval",
                TestCaseID        = "Submit_Vocabularies_For_Approval_04",
                Description       = "Submit vocab that is in Active state (not Draft) → exception thrown",
                ExpectedResult    = "Exception thrown → rollback → return 500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyStatus = Active", "Transaction rollback", "Return 500" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Submit_Vocabularies_For_Approval_05 | N | Valid Draft vocab → PendingApproval → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidDraftVocab_ShouldSetPendingApprovalAndReturn200()
        {
            // Arrange
            var command = new SubmitVocabulariesForApprovalCommand
            {
                VocabularyIds = new List<string> { "VOCAB-003" }
            };
            var draftVocab = MockVocabularyRepository.GetSampleVocabDraft();
            var mockVocabRepo = MockVocabularyRepository.GetMock(returnedVocab: draftVocab);
            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            draftVocab.Status.Should().Be(VocabularyStatus.PendingApproval);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "Submit Vocabularies For Approval",
                TestCaseID        = "Submit_Vocabularies_For_Approval_05",
                Description       = "Submit valid Draft vocab → Status updated to PendingApproval",
                ExpectedResult    = "Status = PendingApproval, return 200",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Draft", "Valid UserId", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Submit_Vocabularies_For_Approval_06 | N | Multiple Draft vocabs → all PendingApproval → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MultipleDraftVocabs_ShouldSetAllPendingAndReturn200()
        {
            // Arrange
            var command = new SubmitVocabulariesForApprovalCommand
            {
                VocabularyIds = new List<string> { "VOCAB-003", "VOCAB-005" }
            };
            var draft1 = MockVocabularyRepository.GetSampleVocabDraft("VOCAB-003");
            var draft2 = MockVocabularyRepository.GetSampleVocabDraft("VOCAB-005");

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetByIdAsync("VOCAB-003")).ReturnsAsync(draft1);
            mockVocabRepo.Setup(x => x.GetByIdAsync("VOCAB-005")).ReturnsAsync(draft2);

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            draft1.Status.Should().Be(VocabularyStatus.PendingApproval);
            draft2.Status.Should().Be(VocabularyStatus.PendingApproval);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup     = "Submit Vocabularies For Approval",
                TestCaseID        = "Submit_Vocabularies_For_Approval_06",
                Description       = "Submit 2 Draft vocabs → both updated to PendingApproval → 200",
                ExpectedResult    = "Both vocab.Status = PendingApproval, return 200",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "2 Draft vocabs", "Batch submit", "Return 200" }
            });
        }
    }
}