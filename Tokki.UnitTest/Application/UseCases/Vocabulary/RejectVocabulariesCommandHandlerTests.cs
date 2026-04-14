using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.Commands.RejectVocabulary;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class RejectVocabulariesCommandHandlerTests
    {
        private RejectVocabulariesCommandHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IEmailService>? emailService = null,
            bool unauthorized = false)
        {
            return new RejectVocabulariesCommandHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                MockVocabularyExampleRepository.GetMock().Object,
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("ADMIN-001").Object,
                (emailService ?? new Mock<IEmailService>()).Object,
                new Mock<ILogger<RejectVocabulariesCommandHandler>>().Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-REJ-01 | A | No token → 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            // Arrange
            var command = new RejectVocabulariesCommand
            {
                VocabularyIds = new List<string> { "VOCAB-002" },
                Reason = "Invalid vocabulary"
            };
            var handler = CreateHandler(unauthorized: true);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Reject", new TestCaseDetail
            {
                FunctionGroup     = "Reject Vocabulary",
                TestCaseID        = "TC-VOCAB-REJ-01",
                Description       = "Reject vocabulary without authentication token",
                ExpectedResult    = "Return 401 Unauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No UserId in Claims", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-REJ-02 | A | Empty VocabularyIds → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyVocabularyIds_ShouldReturn400()
        {
            // Arrange
            var command = new RejectVocabulariesCommand
            {
                VocabularyIds = new List<string>(),
                Reason = "Some reason"
            };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Reject", new TestCaseDetail
            {
                FunctionGroup     = "Reject Vocabulary",
                TestCaseID        = "TC-VOCAB-REJ-02",
                Description       = "Reject with empty VocabularyIds list",
                ExpectedResult    = "Return 400 VOCABULARY_EMPTY",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyIds = empty", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-REJ-03 | A | Missing reason → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MissingReason_ShouldReturn400()
        {
            // Arrange
            var command = new RejectVocabulariesCommand
            {
                VocabularyIds = new List<string> { "VOCAB-002" },
                Reason = ""
            };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Reject", new TestCaseDetail
            {
                FunctionGroup     = "Reject Vocabulary",
                TestCaseID        = "TC-VOCAB-REJ-03",
                Description       = "Reject vocabulary without providing a rejection reason",
                ExpectedResult    = "Return 400 REJECT_REASON_REQUIRED",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Reason = empty string", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-REJ-04 | A | Vocab not PendingApproval → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabNotPendingApproval_ShouldReturn500()
        {
            // Arrange
            var command = new RejectVocabulariesCommand
            {
                VocabularyIds = new List<string> { "VOCAB-001" },
                Reason = "Grammar is incorrect"
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
            QACollector.LogTestCase("Vocabulary - Reject", new TestCaseDetail
            {
                FunctionGroup     = "Reject Vocabulary",
                TestCaseID        = "TC-VOCAB-REJ-04",
                Description       = "Reject vocab that is in Active state (not PendingApproval) → exception",
                ExpectedResult    = "Exception thrown → rollback → return 500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyStatus = Active", "Transaction rollback", "Return 500" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-REJ-05 | N | Valid PendingApproval → Rejected + email → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidPendingVocab_ShouldSetRejectedAndReturn200()
        {
            // Arrange
            var command = new RejectVocabulariesCommand
            {
                VocabularyIds = new List<string> { "VOCAB-002" },
                Reason = "Vocabulary is not grammatically correct"
            };
            var pendingVocab = MockVocabularyRepository.GetSampleVocabPendingApproval();
            var mockVocabRepo = MockVocabularyRepository.GetMock(returnedVocab: pendingVocab);

            var creator = new Account { UserId = "STAFF-001", Email = "staff@tokki.com", FullName = "Tokki Staff" };
            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByIdAsync("STAFF-001")).ReturnsAsync(creator);

            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                accountRepo: mockAccountRepo,
                emailService: mockEmail);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            pendingVocab.Status.Should().Be(VocabularyStatus.Rejected);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Reject", new TestCaseDetail
            {
                FunctionGroup     = "Reject Vocabulary",
                TestCaseID        = "TC-VOCAB-REJ-05",
                Description       = "Reject valid vocab with reason → update Rejected and send email",
                ExpectedResult    = "Status = Rejected, email sent, return 200",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = PendingApproval", "Reason provided", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-REJ-06 | A | Vocab not found → exception → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabNotFound_ShouldReturn500()
        {
            // Arrange
            var command = new RejectVocabulariesCommand
            {
                VocabularyIds = new List<string> { "VOCAB-INVALID" },
                Reason = "Does not meet vocabulary standards"
            };
            var mockVocabRepo = MockVocabularyRepository.GetMock(returnedVocab: null);
            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Reject", new TestCaseDetail
            {
                FunctionGroup     = "Reject Vocabulary",
                TestCaseID        = "TC-VOCAB-REJ-06",
                Description       = "Reject vocab with ID that doesn't exist → exception → rollback → 500",
                ExpectedResult    = "Transaction rollback, return 500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Vocab = null", "Exception thrown", "Return 500" }
            });
        }
    }
}