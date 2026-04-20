using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.Queries.GetByIdForUser;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class GetVocabularyDetailByIdForAdminQueryHandlerTests
    {
        private GetVocabularyDetailByIdForAdminQueryHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null)
        {
            return new GetVocabularyDetailByIdForAdminQueryHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Vocabulary_Detail_By_Id_Admin_01 | A | Vocab not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabNotFound_ShouldReturn404()
        {
            // Arrange
            var query = new GetVocabularyDetailByIdForAdminQuery { VocabularyId = "VOCAB-INVALID" };
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: null));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get Detail (Admin)", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Detail By Id (Admin)",
                TestCaseID        = "Get_Vocabulary_Detail_By_Id_Admin_01",
                Description       = "Admin get vocab detail with non-existent ID",
                ExpectedResult    = "Return 404 VocabularyNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid VocabularyId", "Vocab = null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Vocabulary_Detail_By_Id_Admin_02 | N | Active vocab → 200 with full admin DTO
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidActiveVocab_ShouldReturn200WithAdminDto()
        {
            // Arrange
            var query = new GetVocabularyDetailByIdForAdminQuery { VocabularyId = "VOCAB-001" };
            var vocab = MockVocabularyRepository.GetSampleVocabWithChildren(status: VocabularyStatus.Active);
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: vocab));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.VocabularyId.Should().Be("VOCAB-001");
            result.Data.Status.Should().Be(VocabularyStatus.Active);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get Detail (Admin)", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Detail By Id (Admin)",
                TestCaseID        = "Get_Vocabulary_Detail_By_Id_Admin_02",
                Description       = "Admin get valid Active vocab → return full admin DTO with audit fields",
                ExpectedResult    = "Return 200, Data.VocabularyId = VOCAB-001",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid VocabularyId", "Status = Active", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Vocabulary_Detail_By_Id_Admin_03 | N | Deleted vocab → 200 (admin can see deleted)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DeletedVocab_AdminCanStillSeeIt_ShouldReturn200()
        {
            // Arrange
            var query = new GetVocabularyDetailByIdForAdminQuery { VocabularyId = "VOCAB-004" };
            var vocab = MockVocabularyRepository.GetSampleVocabDeleted();
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: vocab));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Status.Should().Be(VocabularyStatus.Deleted);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get Detail (Admin)", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Detail By Id (Admin)",
                TestCaseID        = "Get_Vocabulary_Detail_By_Id_Admin_03",
                Description       = "Admin get Deleted vocab → admin endpoint does NOT block Deleted, returns 200",
                ExpectedResult    = "Return 200, Status = Deleted",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Deleted", "Admin endpoint", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Vocabulary_Detail_By_Id_Admin_04 | N | PendingApproval vocab → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PendingApprovalVocab_ShouldReturn200()
        {
            // Arrange
            var query = new GetVocabularyDetailByIdForAdminQuery { VocabularyId = "VOCAB-002" };
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
            QACollector.LogTestCase("Vocabulary - Get Detail (Admin)", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Detail By Id (Admin)",
                TestCaseID        = "Get_Vocabulary_Detail_By_Id_Admin_04",
                Description       = "Admin get PendingApproval vocab → accessible → 200",
                ExpectedResult    = "Return 200, Data.Status = PendingApproval",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = PendingApproval", "ID valid", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Vocabulary_Detail_By_Id_Admin_05 | N | DTO has CreateBy and CreateDate audit fields
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidVocab_ShouldIncludeAuditFieldsInDto()
        {
            // Arrange
            var query = new GetVocabularyDetailByIdForAdminQuery { VocabularyId = "VOCAB-001" };
            var vocab = MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active);
            vocab.CreateBy   = "USER-001";
            vocab.CreateDate = new DateTime(2025, 1, 1);
            vocab.UpdateBy   = "ADMIN-001";
            vocab.UpdateDate = new DateTime(2025, 6, 1);

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: vocab));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.CreateBy.Should().Be("USER-001");
            result.Data.UpdateBy.Should().Be("ADMIN-001");

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get Detail (Admin)", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Detail By Id (Admin)",
                TestCaseID        = "Get_Vocabulary_Detail_By_Id_Admin_05",
                Description       = "Admin DTO includes CreateBy, CreateDate, UpdateBy, UpdateDate fields",
                ExpectedResult    = "Return 200, CreateBy = USER-001, UpdateBy = ADMIN-001",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Audit fields populated", "Admin-specific DTO", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_Vocabulary_Detail_By_Id_Admin_06 | N | Vocab with only Deleted examples → Examples list empty in DTO
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabWithOnlyDeletedExamples_ShouldReturnEmptyExamples()
        {
            // Arrange
            var query = new GetVocabularyDetailByIdForAdminQuery { VocabularyId = "VOCAB-001" };
            var vocab = MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active);
            vocab.VocabularyExamples = new List<Tokki.Domain.Entities.VocabularyExample>
            {
                new Tokki.Domain.Entities.VocabularyExample
                {
                    ExampleId    = "EX-DELETED",
                    VocabularyId = "VOCAB-001",
                    Sentence     = "Deleted sentence",
                    Status       = VocabularyExampleStatus.Deleted
                }
            };

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: vocab));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Examples.Should().BeEmpty();

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Get Detail (Admin)", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Detail By Id (Admin)",
                TestCaseID        = "Get_Vocabulary_Detail_By_Id_Admin_06",
                Description       = "Vocab with only Deleted examples → Admin DTO shows empty Examples list",
                ExpectedResult    = "Return 200, Examples = empty list",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "All examples Deleted", "Examples filter = Active", "Return 200" }
            });
        }
    }
}