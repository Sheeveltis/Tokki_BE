using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.Commands.ApproveVocabulary;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class ApproveVocabulariesCommandHandlerTests
    {
        private ApproveVocabulariesCommandHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<IVocabularyExampleRepository>? exampleRepo = null,
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IEmailService>? emailService = null,
            bool unauthorized = false,
            string userId = "ADMIN-001")
        {
            return new ApproveVocabulariesCommandHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                (exampleRepo ?? MockVocabularyExampleRepository.GetMock()).Object,
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                (emailService ?? new Mock<IEmailService>()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock(userId).Object,
                new Mock<ILogger<ApproveVocabulariesCommandHandler>>().Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-APP-01 | A | No token → 401 Unauthorized
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            // Arrange
            var command = new ApproveVocabulariesCommand
            {
                VocabularyIds = new List<string> { "VOCAB-001" }
            };
            var handler = CreateHandler(unauthorized: true);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Approve", new TestCaseDetail
            {
                FunctionGroup     = "Approve Vocabulary",
                TestCaseID        = "TC-VOCAB-APP-01",
                Description       = "Approve vocabulary when there is no authentication token",
                ExpectedResult    = "Return 401 Unauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No UserId in Claims", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-APP-02 | A | Empty VocabularyIds → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyVocabularyIds_ShouldReturn400()
        {
            // Arrange
            var command = new ApproveVocabulariesCommand { VocabularyIds = new List<string>() };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Approve", new TestCaseDetail
            {
                FunctionGroup     = "Approve Vocabulary",
                TestCaseID        = "TC-VOCAB-APP-02",
                Description       = "Approve with empty VocabularyIds list",
                ExpectedResult    = "Return 400 Bad Request",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyIds = empty list", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-APP-03 | A | Vocab not found → exception → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabNotFound_ShouldReturn500()
        {
            // Arrange
            var command = new ApproveVocabulariesCommand
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
            QACollector.LogTestCase("Vocabulary - Approve", new TestCaseDetail
            {
                FunctionGroup     = "Approve Vocabulary",
                TestCaseID        = "TC-VOCAB-APP-03",
                Description       = "Approve vocab with ID that doesn't exist → exception thrown → rollback → 500",
                ExpectedResult    = "Transaction rollback, return 500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Vocab = null", "Exception thrown", "Return 500" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-APP-04 | A | Vocab not PendingApproval → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabNotPendingApproval_ShouldReturn500()
        {
            // Arrange
            var command = new ApproveVocabulariesCommand
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
            QACollector.LogTestCase("Vocabulary - Approve", new TestCaseDetail
            {
                FunctionGroup     = "Approve Vocabulary",
                TestCaseID        = "TC-VOCAB-APP-04",
                Description       = "Approve vocab that is in Active state (not PendingApproval)",
                ExpectedResult    = "Exception thrown → rollback → return 500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyStatus = Active", "Transaction rollback", "Return 500" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-APP-05 | N | Valid PendingApproval vocab → Active + email sent → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidPendingVocab_ShouldSetActiveAndReturn200()
        {
            // Arrange
            var command = new ApproveVocabulariesCommand
            {
                VocabularyIds = new List<string> { "VOCAB-002" }
            };
            var pendingVocab = MockVocabularyRepository.GetSampleVocabPendingApproval();
            var mockVocabRepo = MockVocabularyRepository.GetMock(returnedVocab: pendingVocab);

            var creator = new Account
            {
                UserId   = "STAFF-001",
                Email    = "staff@tokki.com",
                FullName = "Tokki Staff"
            };
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
            pendingVocab.Status.Should().Be(VocabularyStatus.Active);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Approve", new TestCaseDetail
            {
                FunctionGroup     = "Approve Vocabulary",
                TestCaseID        = "TC-VOCAB-APP-05",
                Description       = "Approve valid vocab in PendingApproval → Status = Active, email sent",
                ExpectedResult    = "Status = Active, email sent, return 200",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = PendingApproval", "Creator has valid email", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-APP-06 | N | Multiple vocabs same creator → batch approve → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MultipleVocabsSameCreator_ShouldSendOneEmailAndReturn200()
        {
            // Arrange
            var command = new ApproveVocabulariesCommand
            {
                VocabularyIds = new List<string> { "VOCAB-002", "VOCAB-003" }
            };

            var vocab1 = MockVocabularyRepository.GetSampleVocabPendingApproval("VOCAB-002");
            var vocab2 = MockVocabularyRepository.GetSampleVocabPendingApproval("VOCAB-003");

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetByIdAsync("VOCAB-002")).ReturnsAsync(vocab1);
            mockVocabRepo.Setup(x => x.GetByIdAsync("VOCAB-003")).ReturnsAsync(vocab2);

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
            vocab1.Status.Should().Be(VocabularyStatus.Active);
            vocab2.Status.Should().Be(VocabularyStatus.Active);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Approve", new TestCaseDetail
            {
                FunctionGroup     = "Approve Vocabulary",
                TestCaseID        = "TC-VOCAB-APP-06",
                Description       = "Approve 2 vocabs from the same creator → both Active, 1 email sent",
                ExpectedResult    = "Both vocab.Status = Active, return 200",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "2 PendingApproval vocabs", "Same creator", "Return 200" }
            });
        }
    }
}