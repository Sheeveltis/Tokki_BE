using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.ApproveQuestionBank;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks
{
    public class ApproveQuestionBanksCommandHandlerTests
    {
        private static ApproveQuestionBanksCommandHandler CreateHandler(
            Mock<IQuestionBankRepository>? qbRepo     = null,
            Mock<IAccountRepository>?      accountRepo = null,
            Mock<IEmailService>?           emailSvc    = null,
            Mock<IHttpContextAccessor>?    httpCtx    = null)
        {
            var logger = new Mock<ILogger<ApproveQuestionBanksCommandHandler>>();
            return new ApproveQuestionBanksCommandHandler(
                (qbRepo      ?? MockQuestionBankRepository.GetMock()).Object,
                (accountRepo ?? new Mock<IAccountRepository>()).Object,
                (emailSvc    ?? new Mock<IEmailService>()).Object,
                (httpCtx     ?? MockHttpContextAccessor.GetMock()).Object,
                logger.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-01 | A | No currentUserId in HttpContext → 401 Unauthorized
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoCurrentUser_ShouldReturn401()
        {
            // Arrange
            var httpCtx = MockHttpContextAccessor.GetUnauthorizedMock(); // no user claims
            var handler = CreateHandler(httpCtx: httpCtx);
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "QB-001" } };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Question Bank - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanks",
                TestCaseID        = "TC-QB-APP-01",
                Description       = "No authenticated user in HttpContext → 401 Unauthorized",
                ExpectedResult    = "IsSuccess=false, StatusCode=401",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "HttpContext has no NameIdentifier claim", "401 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-02 | A | Empty QuestionBankIds list → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyIds_ShouldReturn400()
        {
            // Arrange
            var handler = CreateHandler();
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string>() };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanks",
                TestCaseID        = "TC-QB-APP-02",
                Description       = "Empty QuestionBankIds list after dedup → 400 ValidationFailed",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ids.Count == 0", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-03 | A | One ID not found in DB → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_IdNotFound_ShouldReturn404()
        {
            // Arrange — GetByIdsWithDetailsAsync returns empty (no QB found)
            var qbRepo = MockQuestionBankRepository.GetMock(returnedByIdsWithDetails: new List<QuestionBank>());
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "QB-MISSING" } };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanks",
                TestCaseID        = "TC-QB-APP-03",
                Description       = "One QuestionBankId not found in DB → 404 QuestionBankNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdsWithDetailsAsync returns no matching QB", "404 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-04 | A | QB is Deleted → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DeletedQB_ShouldReturn400()
        {
            // Arrange
            var deletedQb = MockQuestionBankRepository.GetSampleDeletedQB("QB-DEL-01");
            var qbRepo    = MockQuestionBankRepository.GetMock(
                returnedByIdsWithDetails: new List<QuestionBank> { deletedQb });
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "QB-DEL-01" } };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanks",
                TestCaseID        = "TC-QB-APP-04",
                Description       = "QB is Deleted status → cannot approve → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status == Deleted", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-05 | A | QB not in PendingApproval (e.g. Draft) → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_QBInDraftStatus_ShouldReturn400()
        {
            // Arrange
            var draftQb = MockQuestionBankRepository.GetSampleDraftQB("QB-DRAFT-01");
            var qbRepo  = MockQuestionBankRepository.GetMock(
                returnedByIdsWithDetails: new List<QuestionBank> { draftQb });
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "QB-DRAFT-01" } };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanks",
                TestCaseID        = "TC-QB-APP-05",
                Description       = "QB is Draft (not PendingApproval) → cannot approve → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status == Draft", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-06 | N | Happy path: PendingApproval QB → Active, 200, email sent
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PendingApprovalQB_ShouldApproveAndReturn200()
        {
            // Arrange
            var pendingQb = MockQuestionBankRepository.GetSamplePendingQB("QB-001");
            var qbRepo    = MockQuestionBankRepository.GetMock(
                returnedByIdsWithDetails: new List<QuestionBank> { pendingQb });

            var account = new Domain.Entities.Account
            {
                UserId = "STAFF-001",
                Email     = "staff@tokki.com",
                FullName  = "Staff User"
            };
            var accountRepo = new Mock<IAccountRepository>();
            accountRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(account);

            var emailSvc = new Mock<IEmailService>();
            emailSvc.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

            var handler = CreateHandler(qbRepo: qbRepo, accountRepo: accountRepo, emailSvc: emailSvc);
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "QB-001" } };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Contain("QB-001");
            qbRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<IEnumerable<QuestionBank>>()), Times.Once);
            qbRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanks",
                TestCaseID        = "TC-QB-APP-06",
                Description       = "Happy path: PendingApproval QB → approved (Active), 200, UpdateRangeAsync+SaveChanges called",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, Data contains QB-001",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=PendingApproval", "current user present", "200 returned, email sent" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-07 | N | Idempotent: Already Active QB → included in result, no DB update
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AlreadyActiveQB_ShouldReturnIdempotentSuccess()
        {
            // Arrange — QB already Active
            var activeQb = MockQuestionBankRepository.GetSampleActiveQB("QB-002");
            var qbRepo   = MockQuestionBankRepository.GetMock(
                returnedByIdsWithDetails: new List<QuestionBank> { activeQb });
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "QB-002" } };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert: still success, QB-002 in result, no UpdateRange called
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain("QB-002");
            qbRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<IEnumerable<QuestionBank>>()), Times.Never);

            QACollector.LogTestCase("Question Bank - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanks",
                TestCaseID        = "TC-QB-APP-07",
                Description       = "Idempotent: QB already Active → included in approvedIds, UpdateRangeAsync not called",
                ExpectedResult    = "IsSuccess=true, Data contains QB-002, UpdateRangeAsync Times.Never",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Active", "idempotent path", "no DB write" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-APP-08 | A | Repository throws exception → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            // Arrange
            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetByIdsWithDetailsAsync(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("DB error"));

            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "QB-001" } };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert: caught internally, returns 500
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Question Bank - Approve", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanks",
                TestCaseID        = "TC-QB-APP-08",
                Description       = "Repository throws exception → caught in try/catch → 500 ServerError",
                ExpectedResult    = "IsSuccess=false, StatusCode=500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdsWithDetailsAsync throws", "catch block returns 500" }
            });
        }
    }
}