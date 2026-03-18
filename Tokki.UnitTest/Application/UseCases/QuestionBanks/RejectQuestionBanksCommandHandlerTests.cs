using FluentAssertions;
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
        private RejectQuestionBanksCommandHandler CreateHandler(
            Mock<IQuestionBankRepository>? qbRepo = null,
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IEmailService>? emailService = null,
            bool unauthorized = false)
        {
            return new RejectQuestionBanksCommandHandler(
                (qbRepo ?? MockQuestionBankRepository.GetMock()).Object,
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                (emailService ?? new Mock<IEmailService>()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("ADMIN-001").Object,
                new Mock<ILogger<RejectQuestionBanksCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_MissingRejectReason_ShouldReturn400()
        {
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-001" },
                RejectReason = ""
            };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup = "Reject Question Banks",
                TestCaseID = "TC-QB-REJ-01",
                Description = "Reject QB mà không nhập lý do từ chối",
                ExpectedResult = "Return 400 REJECT_REASON_REQUIRED",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "RejectReason = empty string",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_QBAlreadyDeleted_ShouldReturn400()
        {
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-003" },
                RejectReason = "Nội dung không phù hợp"
            };

            var deletedQb = MockQuestionBankRepository.GetSampleDeletedQB("QB-003");

            var mockQbRepo = MockQuestionBankRepository.GetMock(
                returnedByIds: new List<QuestionBank> { deletedQb });

            var handler = CreateHandler(qbRepo: mockQbRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup = "Reject Question Banks",
                TestCaseID = "TC-QB-REJ-02",
                Description = "Reject QB đã bị xóa (Status = Deleted)",
                ExpectedResult = "Return 400 ValidationFailed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = Deleted",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_AlreadyRejected_ShouldReturnIdempotent200()
        {
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-004" },
                RejectReason = "Lý do"
            };

            var rejectedQb = new QuestionBank
            {
                QuestionBankId = "QB-004",
                Status = QuestionBankStatus.Rejected,
                CreateBy = "STAFF-001",
                QuestionOptions = new List<QuestionOption>()
            };

            var mockQbRepo = MockQuestionBankRepository.GetMock(
                returnedByIds: new List<QuestionBank> { rejectedQb });

            var handler = CreateHandler(qbRepo: mockQbRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Contain("QB-004");

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup = "Reject Question Banks",
                TestCaseID = "TC-QB-REJ-03",
                Description = "Reject QB đã Rejected → idempotent, return 200",
                ExpectedResult = "Return 200, QB-004 trong rejectedIds",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = Rejected (boundary: đã reject)",
                    "Idempotent → return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidPendingQB_ShouldRejectAndSendEmailAndReturn200()
        {
            var command = new RejectQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-001" },
                RejectReason = "Câu hỏi không rõ ràng"
            };

            var pendingQb = MockQuestionBankRepository.GetSamplePendingQB("QB-001");

            var mockQbRepo = MockQuestionBankRepository.GetMock(
                returnedByIds: new List<QuestionBank> { pendingQb });

            var creator = new Account
            {
                UserId = "STAFF-001",
                Email = "staff@tokki.com",
                FullName = "Tokki Staff"
            };

            var mockAccountRepo = MockAccountRepository.GetMock();
            mockAccountRepo.Setup(x => x.GetByIdAsync("STAFF-001"))
                           .ReturnsAsync(creator);

            var mockEmail = new Mock<IEmailService>();
            mockEmail.Setup(x => x.SendEmailAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            var handler = CreateHandler(
                qbRepo: mockQbRepo,
                accountRepo: mockAccountRepo,
                emailService: mockEmail);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Contain("QB-001");
            pendingQb.Status.Should().Be(QuestionBankStatus.Rejected);

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup = "Reject Question Banks",
                TestCaseID = "TC-QB-REJ-04",
                Description = "Reject QB PendingApproval hợp lệ → Status = Rejected, gửi email, return 200",
                ExpectedResult = "Status = Rejected, email sent, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = PendingApproval",
                    "RejectReason provided",
                    "Creator has valid email",
                    "Return 200"
                }
            });
        }
    }
}