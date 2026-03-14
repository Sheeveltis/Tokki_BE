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

        [Fact]
        public async Task Handle_VocabNotDraft_ShouldReturn500()
        {
            var command = new SubmitVocabulariesForApprovalCommand
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

            QACollector.LogTestCase("Vocabulary - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup = "Submit Vocabularies For Approval",
                TestCaseID = "TC-VOCAB-SFA-01",
                Description = "Submit vocab đang ở trạng thái Active (không phải Draft)",
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
        public async Task Handle_ValidDraftVocab_ShouldSetPendingApprovalAndReturn200()
        {
            var command = new SubmitVocabulariesForApprovalCommand
            {
                VocabularyIds = new List<string> { "VOCAB-003" }
            };

            var draftVocab = MockVocabularyRepository.GetSampleVocabDraft();
            var mockVocabRepo = MockVocabularyRepository.GetMock(returnedVocab: draftVocab);

            var handler = CreateHandler(vocabRepo: mockVocabRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            draftVocab.Status.Should().Be(VocabularyStatus.PendingApproval);

            QACollector.LogTestCase("Vocabulary - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup = "Submit Vocabularies For Approval",
                TestCaseID = "TC-VOCAB-SFA-02",
                Description = "Submit vocab Draft hợp lệ → cập nhật PendingApproval",
                ExpectedResult = "Status = PendingApproval, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = Draft",
                    "Valid UserId",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_EmptyVocabularyIds_ShouldReturn400()
        {
            var command = new SubmitVocabulariesForApprovalCommand
            {
                VocabularyIds = new List<string>()
            };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Vocabulary - Submit For Approval", new TestCaseDetail
            {
                FunctionGroup = "Submit Vocabularies For Approval",
                TestCaseID = "TC-VOCAB-SFA-03",
                Description = "Submit với danh sách rỗng",
                ExpectedResult = "Return 400 VOCABULARY_EMPTY",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyIds = empty", "Return 400" }
            });
        }
    }
}