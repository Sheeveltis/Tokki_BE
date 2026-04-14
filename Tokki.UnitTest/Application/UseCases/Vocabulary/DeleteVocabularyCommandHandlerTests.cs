using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.Commands.DeleteVocabulary;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class DeleteVocabularyCommandHandlerTests
    {
        private DeleteVocabularyCommandHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            bool unauthorized = false)
        {
            return new DeleteVocabularyCommandHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                new Mock<IVocabularyTopicRepository>().Object,
                MockVocabularyExampleRepository.GetMock().Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("ADMIN-001").Object,
                new Mock<ILogger<DeleteVocabularyCommandHandler>>().Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-DEL-01 | A | No token → 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            // Arrange
            var command = new DeleteVocabularyCommand { VocabularyId = "VOCAB-001" };
            var handler = CreateHandler(unauthorized: true);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Vocabulary",
                TestCaseID        = "TC-VOCAB-DEL-01",
                Description       = "Delete vocabulary without authentication token",
                ExpectedResult    = "Return 401 Unauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No UserId in Claims", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-DEL-02 | A | Vocab not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabNotFound_ShouldReturn404()
        {
            // Arrange
            var command = new DeleteVocabularyCommand { VocabularyId = "VOCAB-INVALID" };
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocabWithChildren: null));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Vocabulary",
                TestCaseID        = "TC-VOCAB-DEL-02",
                Description       = "Delete vocabulary with non-existing ID",
                ExpectedResult    = "Return 404 VocabularyNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid VocabularyId", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-DEL-03 | A | Vocab already Deleted → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabAlreadyDeleted_ShouldReturn400()
        {
            // Arrange
            var command = new DeleteVocabularyCommand { VocabularyId = "VOCAB-004" };
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocabWithChildren: MockVocabularyRepository.GetSampleVocabDeleted()));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Vocabulary",
                TestCaseID        = "TC-VOCAB-DEL-03",
                Description       = "Delete previously deleted vocabulary (Status = Deleted)",
                ExpectedResult    = "Return 400 VocabularyAlreadyDeleted",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Deleted", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-DEL-04 | N | Valid Active vocab → soft delete cascade → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidVocab_ShouldSoftDeleteCascadeAndReturn200()
        {
            // Arrange
            var command = new DeleteVocabularyCommand { VocabularyId = "VOCAB-001" };
            var vocabWithChildren = MockVocabularyRepository.GetSampleVocabWithChildren();

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocabWithChildren: vocabWithChildren));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            vocabWithChildren.Status.Should().Be(VocabularyStatus.Deleted);
            vocabWithChildren.VocabularyTopics.Should().OnlyContain(vt => vt.Status == VocabularyTopicStatus.Deleted);
            vocabWithChildren.VocabularyExamples.Should().OnlyContain(ex => ex.Status == VocabularyExampleStatus.Deleted);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Vocabulary",
                TestCaseID        = "TC-VOCAB-DEL-04",
                Description       = "Delete valid vocab → soft delete cascades to Topics and Examples",
                ExpectedResult    = "Vocab + Topics + Examples all Status = Deleted, return 200",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid VocabularyId", "Has Topics and Examples", "Cascade soft delete", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-DEL-05 | N | Vocab without children → soft delete vocab only → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabWithNoChildren_ShouldSoftDeleteAndReturn200()
        {
            // Arrange
            var command = new DeleteVocabularyCommand { VocabularyId = "VOCAB-001" };
            var vocab = MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active);

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocabWithChildren: vocab));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            vocab.Status.Should().Be(VocabularyStatus.Deleted);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Vocabulary",
                TestCaseID        = "TC-VOCAB-DEL-05",
                Description       = "Delete vocab with no topic/example children → soft delete vocab only",
                ExpectedResult    = "Vocab.Status = Deleted, return 200",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Vocab has no children", "Only vocab soft-deleted", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VOCAB-DEL-06 | A | Repository throws → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            // Arrange
            var command = new DeleteVocabularyCommand { VocabularyId = "VOCAB-001" };
            var vocab = MockVocabularyRepository.GetSampleVocabulary("VOCAB-001", VocabularyStatus.Active);
            var mockVocabRepo = MockVocabularyRepository.GetMock(returnedVocabWithChildren: vocab);
            mockVocabRepo.Setup(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.Vocabulary>()))
                         .ThrowsAsync(new Exception("DB update failed"));

            var handler = CreateHandler(vocabRepo: mockVocabRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            // Excel Log
            QACollector.LogTestCase("Vocabulary - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Vocabulary",
                TestCaseID        = "TC-VOCAB-DEL-06",
                Description       = "Repository.UpdateAsync throws exception → return 500",
                ExpectedResult    = "Return 500 Server Error",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UpdateAsync throws DB exception", "Return 500" }
            });
        }
    }
}