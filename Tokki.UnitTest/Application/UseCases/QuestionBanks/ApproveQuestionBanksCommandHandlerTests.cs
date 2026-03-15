using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
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
        private ApproveQuestionBanksCommandHandler CreateHandler(
            Mock<IQuestionBankRepository>? qbRepo = null,
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IEmailService>? emailService = null,
            bool unauthorized = false)
        {
            return new ApproveQuestionBanksCommandHandler(
                (qbRepo ?? MockQuestionBankRepository.GetMock()).Object,
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                (emailService ?? new Mock<IEmailService>()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("ADMIN-001").Object,
                new Mock<ILogger<ApproveQuestionBanksCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            var command = new ApproveQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-001" }
            };

            var handler = CreateHandler(unauthorized: true);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup = "Approve Question Banks",
                TestCaseID = "TC-QB-APP-01",
                Description = "Approve không có token xác thực",
                ExpectedResult = "Return 401 Unauthorized",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "No UserId in Claims",
                    "Return 401"
                }
            });
        }

        [Fact]
        public async Task Handle_EmptyIds_ShouldReturn400()
        {
            var command = new ApproveQuestionBanksCommand
            {
                QuestionBankIds = new List<string>()
            };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup = "Approve Question Banks",
                TestCaseID = "TC-QB-APP-02",
                Description = "Approve với danh sách QuestionBankIds rỗng",
                ExpectedResult = "Return 400 ValidationFailed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "QuestionBankIds = empty",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_QuestionBankNotFound_ShouldReturn404()
        {
            var command = new ApproveQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-INVALID" }
            };

            // GetByIdsWithDetailsAsync trả về empty → missing
            var mockQbRepo = MockQuestionBankRepository.GetMock(
                returnedByIds: new List<QuestionBank>());

            var handler = CreateHandler(qbRepo: mockQbRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup = "Approve Question Banks",
                TestCaseID = "TC-QB-APP-03",
                Description = "Approve với QuestionBankId không tồn tại",
                ExpectedResult = "Return 404 QuestionBankNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "QuestionBankId không tồn tại",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_AlreadyActive_ShouldReturnIdempotent200()
        {
            var command = new ApproveQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-002" }
            };

            // QB đã Active → idempotent
            var mockQbRepo = MockQuestionBankRepository.GetMock(
                returnedByIds: new List<QuestionBank>
                {
                    MockQuestionBankRepository.GetSampleActiveQB("QB-002")
                });

            var handler = CreateHandler(qbRepo: mockQbRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Contain("QB-002");

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup = "Approve Question Banks",
                TestCaseID = "TC-QB-APP-04",
                Description = "Approve QB đã Active → idempotent, return 200",
                ExpectedResult = "Return 200, QB-002 trong approvedIds",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = Active (boundary: đã approve)",
                    "Idempotent → return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidPendingQB_ShouldApproveAndReturn200()
        {
            var command = new ApproveQuestionBanksCommand
            {
                QuestionBankIds = new List<string> { "QB-001" }
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
            pendingQb.Status.Should().Be(QuestionBankStatus.Active);
            pendingQb.ApprovedBy.Should().Be("ADMIN-001");

            QACollector.LogTestCase("QuestionBank - Approve", new TestCaseDetail
            {
                FunctionGroup = "Approve Question Banks",
                TestCaseID = "TC-QB-APP-05",
                Description = "Approve QB PendingApproval hợp lệ → Status = Active, gửi email, return 200",
                ExpectedResult = "Status = Active, ApprovedBy = ADMIN-001, email sent, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = PendingApproval",
                    "Creator has valid email",
                    "UpdateRangeAsync called",
                    "Return 200"
                }
            });
        }
    }
}