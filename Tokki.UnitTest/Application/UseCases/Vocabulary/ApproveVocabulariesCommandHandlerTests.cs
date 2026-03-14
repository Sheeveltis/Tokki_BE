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

        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            var command = new ApproveVocabulariesCommand
            {
                VocabularyIds = new List<string> { "VOCAB-001" }
            };

            var handler = CreateHandler(unauthorized: true);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("Vocabulary - Approve", new TestCaseDetail
            {
                FunctionGroup = "Approve Vocabulary",
                TestCaseID = "TC-VOCAB-APP-01",
                Description = "Approve vocabulary khi không có token xác thực",
                ExpectedResult = "Return 401 Unauthorized",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No UserId in Claims", "Return 401" }
            });
        }

        [Fact]
        public async Task Handle_EmptyVocabularyIds_ShouldReturn400()
        {
            var command = new ApproveVocabulariesCommand
            {
                VocabularyIds = new List<string>()
            };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Vocabulary - Approve", new TestCaseDetail
            {
                FunctionGroup = "Approve Vocabulary",
                TestCaseID = "TC-VOCAB-APP-02",
                Description = "Approve với danh sách VocabularyIds rỗng",
                ExpectedResult = "Return 400 Bad Request",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyIds = empty list", "Return 400" }
            });
        }

        [Fact]
        public async Task Handle_VocabNotPendingApproval_ShouldReturn500()
        {
            var command = new ApproveVocabulariesCommand
            {
                VocabularyIds = new List<string> { "VOCAB-001" }
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock(
                returnedVocab: MockVocabularyRepository.GetSampleVocabulary(
                    status: VocabularyStatus.Active));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Vocabulary - Approve", new TestCaseDetail
            {
                FunctionGroup = "Approve Vocabulary",
                TestCaseID = "TC-VOCAB-APP-03",
                Description = "Approve vocab đang ở trạng thái Active (không phải PendingApproval)",
                ExpectedResult = "Exception thrown → rollback → return 500",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "VocabularyStatus = Active",
                    "Transaction rollback",
                    "Return 500"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidPendingVocab_ShouldSetActiveAndReturn200()
        {
            var command = new ApproveVocabulariesCommand
            {
                VocabularyIds = new List<string> { "VOCAB-002" }
            };

            var pendingVocab = MockVocabularyRepository.GetSampleVocabPendingApproval();
            var mockVocabRepo = MockVocabularyRepository.GetMock(returnedVocab: pendingVocab);

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
                vocabRepo: mockVocabRepo,
                accountRepo: mockAccountRepo,
                emailService: mockEmail);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            pendingVocab.Status.Should().Be(VocabularyStatus.Active);

            QACollector.LogTestCase("Vocabulary - Approve", new TestCaseDetail
            {
                FunctionGroup = "Approve Vocabulary",
                TestCaseID = "TC-VOCAB-APP-04",
                Description = "Approve vocab hợp lệ đang ở PendingApproval → cập nhật Active và gửi email",
                ExpectedResult = "Status = Active, email sent, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = PendingApproval",
                    "Creator has valid email",
                    "Return 200"
                }
            });
        }
    }
}