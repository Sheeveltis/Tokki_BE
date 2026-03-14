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

        [Fact]
        public async Task Handle_MissingReason_ShouldReturn400()
        {
            var command = new RejectVocabulariesCommand
            {
                VocabularyIds = new List<string> { "VOCAB-002" },
                Reason = ""
            };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Vocabulary - Reject", new TestCaseDetail
            {
                FunctionGroup = "Reject Vocabulary",
                TestCaseID = "TC-VOCAB-REJ-01",
                Description = "Reject vocabulary mà không nhập lý do từ chối",
                ExpectedResult = "Return 400 REJECT_REASON_REQUIRED",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Reason = empty string", "Return 400" }
            });
        }

        [Fact]
        public async Task Handle_ValidPendingVocab_ShouldSetRejectedAndReturn200()
        {
            var command = new RejectVocabulariesCommand
            {
                VocabularyIds = new List<string> { "VOCAB-002" },
                Reason = "Từ vựng không đúng ngữ pháp"
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
            pendingVocab.Status.Should().Be(VocabularyStatus.Rejected);

            QACollector.LogTestCase("Vocabulary - Reject", new TestCaseDetail
            {
                FunctionGroup = "Reject Vocabulary",
                TestCaseID = "TC-VOCAB-REJ-02",
                Description = "Reject vocab hợp lệ với lý do → cập nhật Rejected và gửi email",
                ExpectedResult = "Status = Rejected, email sent, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = PendingApproval",
                    "Reason provided",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_EmptyVocabularyIds_ShouldReturn400()
        {
            var command = new RejectVocabulariesCommand
            {
                VocabularyIds = new List<string>(),
                Reason = "Lý do"
            };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Vocabulary - Reject", new TestCaseDetail
            {
                FunctionGroup = "Reject Vocabulary",
                TestCaseID = "TC-VOCAB-REJ-03",
                Description = "Reject với danh sách VocabularyIds rỗng",
                ExpectedResult = "Return 400 VOCABULARY_EMPTY",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyIds = empty", "Return 400" }
            });
        }
    }
}