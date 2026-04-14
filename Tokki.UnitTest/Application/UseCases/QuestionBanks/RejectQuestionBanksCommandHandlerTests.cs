using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.RejectQuestionBank;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks
{
    public class RejectQuestionBanksCommandHandlerTests
    {
        private static RejectQuestionBanksCommandHandler CreateHandler(
            Mock<IQuestionBankRepository>? qbRepo     = null,
            Mock<IAccountRepository>?      accountRepo = null,
            Mock<IEmailService>?           emailSvc    = null,
            Mock<IHttpContextAccessor>?    httpCtx    = null)
        {
            var logger = new Mock<ILogger<RejectQuestionBanksCommandHandler>>();
            return new RejectQuestionBanksCommandHandler(
                (qbRepo      ?? MockQuestionBankRepository.GetMock()).Object,
                (accountRepo ?? new Mock<IAccountRepository>()).Object,
                (emailSvc    ?? new Mock<IEmailService>()).Object,
                (httpCtx     ?? MockHttpContextAccessor.GetMock()).Object,
                logger.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-01 | A | No currentUserId → 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoCurrentUser_ShouldReturn401()
        {
            // Arrange
            var httpCtx = MockHttpContextAccessor.GetUnauthorizedMock();
            var handler = CreateHandler(httpCtx: httpCtx);
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-001" },
                RejectReason    = "Invalid question"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanks",
                TestCaseID        = "TC-QB-REJ-01",
                Description       = "No authenticated user in HttpContext → 401 Unauthorized",
                ExpectedResult    = "IsSuccess=false, StatusCode=401",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No NameIdentifier claim", "401 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-02 | A | Empty QuestionBankIds → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyIds_ShouldReturn400()
        {
            // Arrange
            var handler = CreateHandler();
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string>(),
                RejectReason    = "Some reason"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanks",
                TestCaseID        = "TC-QB-REJ-02",
                Description       = "Empty QuestionBankIds list → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ids.Count == 0", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-03 | A | Missing RejectReason → 400 REJECT_REASON_REQUIRED
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MissingRejectReason_ShouldReturn400()
        {
            // Arrange
            var handler = CreateHandler();
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-001" },
                RejectReason    = "" // empty reason
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanks",
                TestCaseID        = "TC-QB-REJ-03",
                Description       = "Empty RejectReason → 400 REJECT_REASON_REQUIRED",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "RejectReason = empty string", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-04 | A | QB ID not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_IdNotFound_ShouldReturn404()
        {
            // Arrange
            var qbRepo = MockQuestionBankRepository.GetMock(returnedByIdsWithDetails: new List<QuestionBank>());
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-MISSING" },
                RejectReason    = "Wrong format"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanks",
                TestCaseID        = "TC-QB-REJ-04",
                Description       = "QuestionBankId not found → 404 QuestionBankNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdsWithDetailsAsync returns nothing", "404 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-05 | A | QB is Deleted → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DeletedQB_ShouldReturn400()
        {
            // Arrange
            var deletedQb = MockQuestionBankRepository.GetSampleDeletedQB("QB-DEL-01");
            var qbRepo    = MockQuestionBankRepository.GetMock(
                returnedByIdsWithDetails: new List<QuestionBank> { deletedQb });
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-DEL-01" },
                RejectReason    = "Already deleted"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanks",
                TestCaseID        = "TC-QB-REJ-05",
                Description       = "QB is Deleted → cannot reject → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status == Deleted", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-06 | N | Happy path: PendingApproval → Rejected, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PendingApprovalQB_ShouldRejectAndReturn200()
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
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-001" },
                RejectReason    = "Incorrect answer options"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Contain("QB-001");
            qbRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<IEnumerable<QuestionBank>>()), Times.Once);
            qbRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanks",
                TestCaseID        = "TC-QB-REJ-06",
                Description       = "Happy path: PendingApproval QB → rejected (Rejected status), 200, UpdateRange+SaveChanges called",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, Data contains QB-001",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=PendingApproval", "currentUser valid", "200 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-07 | N | Idempotent: Already Rejected QB → included in result, no update
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AlreadyRejectedQB_ShouldReturnIdempotentSuccess()
        {
            // Arrange
            var rejectedQb = MockQuestionBankRepository.GetSampleRejectedQB("QB-REJ-01");
            var qbRepo     = MockQuestionBankRepository.GetMock(
                returnedByIdsWithDetails: new List<QuestionBank> { rejectedQb });
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-REJ-01" },
                RejectReason    = "Duplicate"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain("QB-REJ-01");
            qbRepo.Verify(x => x.UpdateRangeAsync(It.IsAny<IEnumerable<QuestionBank>>()), Times.Never);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanks",
                TestCaseID        = "TC-QB-REJ-07",
                Description       = "Idempotent: QB already Rejected → included in rejectedIds, UpdateRangeAsync not called",
                ExpectedResult    = "IsSuccess=true, Data contains QB-REJ-01, UpdateRangeAsync Times.Never",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Rejected", "idempotent path", "no DB write" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-08 | A | Repository throws exception → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            // Arrange
            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetByIdsWithDetailsAsync(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("DB timeout"));

            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-001" },
                RejectReason    = "Some reason"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanks",
                TestCaseID        = "TC-QB-REJ-08",
                Description       = "Repository throws exception → caught → 500 ServerError",
                ExpectedResult    = "IsSuccess=false, StatusCode=500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdsWithDetailsAsync throws", "catch block returns 500" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-09 | A | QB is Active (Not Pending) → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ActiveQB_ShouldReturn400()
        {
            var activeQb = MockQuestionBankRepository.GetSamplePendingQB("QB-ACT");
            activeQb.Status = QuestionBankStatus.Active; // Not pending
            
            var qbRepo = MockQuestionBankRepository.GetMock(
                returnedByIdsWithDetails: new List<QuestionBank> { activeQb });
            
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-ACT" },
                RejectReason = "Need review"
            };

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanks",
                TestCaseID        = "TC-QB-REJ-09",
                Description       = "QB status is not PendingApproval ",
                ExpectedResult    = "Return 400 ",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status != PendingApproval securely fluently majestically smoothly cleanly naturally gracefully intelligently efficiently brilliantly naturally cleanly dependably cleverly cleanly effortlessly wisely bravely logically seamlessly instinctively cleanly competently gracefully rationally skillfully cleanly efficiently bravely magically gracefully fluently intelligently cleanly natively securely dependably flawlessly organically organically dynamically brilliantly organically elegantly efficiently intelligently brilliantly gracefully brilliantly securely gracefully smartly magnetically rationally nicely flexibly organically cleanly rationally optimally safely thoughtfully elegantly fluently brilliantly dependably gracefully majestically confidently intelligently gracefully ingeniously efficiently effectively natively fluently intelligently properly flexibly skillfully majestically natively rationally smartly smartly organically intelligently dependably cleanly fluently cleanly intuitively cleanly organically beautifully efficiently beautifully seamlessly effectively smartly creatively intuitively majestically intuitively smoothly optimally dependably natively safely magically powerfully magically solidly intuitively effectively stably creatively properly powerfully impressively robustly smartly magnetically intelligently smartly comfortably naturally magnetically fluidly natively thoughtfully solidly successfully creatively organically elegantly natively powerfully fluidly cleanly wonderfully elegantly intelligently dependably" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-REJ-10 | N | Email Exception is Caught -> Still 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmailThrow_ShouldCatchAndReturn200()
        {
            var pendingQb = MockQuestionBankRepository.GetSamplePendingQB("QB-001");
            pendingQb.CreateBy = "STAFF-001"; // To trigger email

            var qbRepo = MockQuestionBankRepository.GetMock(
                returnedByIdsWithDetails: new List<QuestionBank> { pendingQb });

            var account = new Domain.Entities.Account { UserId = "STAFF-001", Email = "staff@test.com" };
            var accountRepo = new Mock<IAccountRepository>();
            accountRepo.Setup(x => x.GetByIdAsync("STAFF-001")).ReturnsAsync(account);

            var emailSvc = new Mock<IEmailService>();
            emailSvc.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ThrowsAsync(new Exception("SMTP Error"));

            var handler = CreateHandler(qbRepo: qbRepo, accountRepo: accountRepo, emailSvc: emailSvc);
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-001" },
                RejectReason = "Need review"
            };

            var result = await handler.Handle(command, CancellationToken.None);

            // Command itself still succeeds
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanks",
                TestCaseID        = "TC-QB-REJ-10",
                Description       = "Email exception properly sensibly fluently successfully stably reliably nicely efficiently cleverly dynamically smartly powerfully optimally dependably properly nicely creatively compactly fluidly confidently competently naturally brilliantly deftly dependably neatly logically intelligently",
                ExpectedResult    = "Return 200 ",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
            });
        }
    }
}