using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.Queries.GetById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class GetVocabularyDetailByIdQueryHandlerTests
    {
        private GetVocabularyDetailByIdQueryHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null)
        {
            return new GetVocabularyDetailByIdQueryHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GID-01 | A | Vocab not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabNotFound_ShouldReturn404()
        {
            // Arrange
            var query = new GetVocabularyDetailByIdQuery { VocabularyId = "VOCAB-INVALID" };
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: null));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Detail By Id",
                TestCaseID        = "TC-VOCAB-GID-01",
                Description       = "Get vocab detail with ID that doesn't exist",
                ExpectedResult    = "Return 404 VocabularyNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid VocabularyId", "Vocab = null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GID-02 | A | Vocab is Deleted → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabIsDeleted_ShouldReturn404()
        {
            // Arrange
            var query = new GetVocabularyDetailByIdQuery { VocabularyId = "VOCAB-004" };
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocab: MockVocabularyRepository.GetSampleVocabDeleted()));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Detail By Id",
                TestCaseID        = "TC-VOCAB-GID-02",
                Description       = "Get deleted vocab detail (Status = Deleted) → not displayed",
                ExpectedResult    = "Return 404 VocabularyNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyId is valid", "Status = Deleted", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GID-03 | N | Valid Active vocab → 200 with full DTO
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidActiveVocab_ShouldReturn200WithDto()
        {
            // Arrange
            var query = new GetVocabularyDetailByIdQuery { VocabularyId = "VOCAB-001" };
            var vocab = MockVocabularyRepository.GetSampleVocabWithChildren(status: VocabularyStatus.Active);
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: vocab));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.VocabularyId.Should().Be("VOCAB-001");
            result.Data.Text.Should().NotBeNullOrEmpty();

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Detail By Id",
                TestCaseID        = "TC-VOCAB-GID-03",
                Description       = "Get valid Active vocab → return full DTO with topics and examples",
                ExpectedResult    = "Return 200, Data.VocabularyId = VOCAB-001",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid VocabularyId", "Status = Active", "Has topics and examples", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GID-04 | N | PendingApproval vocab → 200 (not blocked for user view)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PendingApprovalVocab_ShouldReturn200()
        {
            // Arrange
            var query = new GetVocabularyDetailByIdQuery { VocabularyId = "VOCAB-002" };
            var vocab = MockVocabularyRepository.GetSampleVocabPendingApproval();
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: vocab));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Status.Should().Be(VocabularyStatus.PendingApproval);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Detail By Id",
                TestCaseID        = "TC-VOCAB-GID-04",
                Description       = "Get PendingApproval vocab detail → accessible (not filtered like Deleted)",
                ExpectedResult    = "Return 200, Data.Status = PendingApproval",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = PendingApproval", "ID valid", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GID-05 | N | Vocab with examples → examples in DTO ordered by CreateAt
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabWithMultipleExamples_ShouldReturnOnlyActiveExamples()
        {
            // Arrange
            var query = new GetVocabularyDetailByIdQuery { VocabularyId = "VOCAB-001" };
            var vocab = MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active);
            vocab.VocabularyExamples = new List<Tokki.Domain.Entities.VocabularyExample>
            {
                new Tokki.Domain.Entities.VocabularyExample { ExampleId = "EX-001", VocabularyId = "VOCAB-001", Sentence = "안녕!", Status = VocabularyExampleStatus.Active, CreateAt = DateTime.UtcNow.AddDays(-2) },
                new Tokki.Domain.Entities.VocabularyExample { ExampleId = "EX-002", VocabularyId = "VOCAB-001", Sentence = "삭제됨", Status = VocabularyExampleStatus.Deleted, CreateAt = DateTime.UtcNow.AddDays(-1) }
            };

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: vocab));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Examples.Should().HaveCount(1);
            result.Data.Examples[0].ExampleId.Should().Be("EX-001");

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Detail By Id",
                TestCaseID        = "TC-VOCAB-GID-05",
                Description       = "Vocab with 2 examples (1 Active, 1 Deleted) → only Active example in DTO",
                ExpectedResult    = "Return 200, Examples.Count = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "2 examples (1 Active, 1 Deleted)", "Filter = Active only", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-GID-06 | N | Draft vocab → 200 (accessible to caller)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DraftVocab_ShouldReturn200()
        {
            // Arrange
            var query = new GetVocabularyDetailByIdQuery { VocabularyId = "VOCAB-003" };
            var vocab = MockVocabularyRepository.GetSampleVocabDraft();
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: vocab));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Status.Should().Be(VocabularyStatus.Draft);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get Detail", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Detail By Id",
                TestCaseID        = "TC-VOCAB-GID-06",
                Description       = "Get Draft vocab detail → accessible (not filtered)",
                ExpectedResult    = "Return 200, Data.Status = Draft",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Draft", "ID valid", "Return 200" }
            });
        }
    }
}