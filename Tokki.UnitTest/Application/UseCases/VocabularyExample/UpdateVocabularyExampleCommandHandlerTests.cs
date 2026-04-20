using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.VocabularyExample.Commands.UpdateExample;
using Tokki.Application.UseCases.VocabularyExample.DTOs;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.VocabularyExample
{
    public class UpdateVocabularyExampleCommandHandlerTests
    {
        private UpdateVocabularyExampleCommandHandler CreateHandler(
            Mock<IVocabularyExampleRepository>? exampleRepo = null,
            bool unauthorized = false)
        {
            return new UpdateVocabularyExampleCommandHandler(
                (exampleRepo ?? MockVocabularyExampleRepository.GetMock()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("USER-001").Object,
                new Mock<ILogger<UpdateVocabularyExampleCommandHandler>>().Object);
        }

        // ═══════════════════════════════════════════════════════════
        // Update_Vocabulary_Example_01 | A | No token → 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            // Arrange
            var command = new UpdateVocabularyExampleCommand
            {
                ExampleId  = "EX-001",
                UpdateData = new VocabularyExampleUpdateDto { Sentence = "새 문장" }
            };
            var handler = CreateHandler(unauthorized: true);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Vocabulary Example",
                TestCaseID        = "Update_Vocabulary_Example_01",
                Description       = "Update example sentence without authentication token",
                ExpectedResult    = "Return 401 Unauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No UserId in Claims", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Update_Vocabulary_Example_02 | A | Example not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExampleNotFound_ShouldReturn404()
        {
            // Arrange
            var command = new UpdateVocabularyExampleCommand
            {
                ExampleId  = "EX-INVALID",
                UpdateData = new VocabularyExampleUpdateDto { Sentence = "새 문장" }
            };
            var handler = CreateHandler(
                exampleRepo: MockVocabularyExampleRepository.GetMock(existingById: null));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Vocabulary Example",
                TestCaseID        = "Update_Vocabulary_Example_02",
                Description       = "Update example with non-existent ExampleId",
                ExpectedResult    = "Return 404 ExampleNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid ExampleId", "Example = null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Update_Vocabulary_Example_03 | A | Sentence duplicate (different example) → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicateSentence_ShouldReturn400()
        {
            // Arrange
            var existingExample = MockVocabularyExampleRepository.GetSampleExample();
            existingExample.ExampleId    = "EX-001";
            existingExample.VocabularyId = "VOCAB-001";
            existingExample.Sentence     = "기존 문장";

            var duplicateExample = MockVocabularyExampleRepository.GetSampleExample();
            duplicateExample.ExampleId = "EX-OTHER"; // different ID → real duplicate

            var command = new UpdateVocabularyExampleCommand
            {
                ExampleId  = "EX-001",
                UpdateData = new VocabularyExampleUpdateDto { Sentence = "중복 문장" }
            };

            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(
                existingById: existingExample,
                existingExample: duplicateExample);
            var handler = CreateHandler(exampleRepo: mockExampleRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Vocabulary Example",
                TestCaseID        = "Update_Vocabulary_Example_03",
                Description       = "Update Sentence to a value that already exists in another example",
                ExpectedResult    = "Return 400 ExampleDuplicate",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "New sentence = existing other example's sentence", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Update_Vocabulary_Example_04 | N | Update Sentence only (unchanged same value) → 200, no dup check
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SameSentence_ShouldNotCheckDuplicateAndReturn200()
        {
            // Arrange
            var example = MockVocabularyExampleRepository.GetSampleExample();
            example.Sentence = "기존 문장";

            // Same sentence → no change, handler skips dup check
            var command = new UpdateVocabularyExampleCommand
            {
                ExampleId  = example.ExampleId,
                UpdateData = new VocabularyExampleUpdateDto { Sentence = "기존 문장" }
            };

            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(existingById: example);
            var handler = CreateHandler(exampleRepo: mockExampleRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            // GetBySentenceAsync should NOT be called (same sentence skips dup check)
            mockExampleRepo.Verify(
                x => x.GetBySentenceAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Vocabulary Example",
                TestCaseID        = "Update_Vocabulary_Example_04",
                Description       = "Update Sentence to same value as current → no duplicate check, returns 200",
                ExpectedResult    = "Return 200, GetBySentenceAsync not called",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Sentence unchanged", "No dup check (boundary)", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Update_Vocabulary_Example_05 | N | Valid new Sentence + Translation → 200 with updated DTO
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidUpdate_ShouldUpdateAndReturn200()
        {
            // Arrange
            var example = MockVocabularyExampleRepository.GetSampleExample();
            example.Sentence    = "기존 문장";
            example.Translation = "Old translation";

            var command = new UpdateVocabularyExampleCommand
            {
                ExampleId  = example.ExampleId,
                UpdateData = new VocabularyExampleUpdateDto
                {
                    Sentence    = "새 문장",
                    Translation = "New translation"
                }
            };

            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(
                existingById: example,
                existingExample: null); // no dup
            var handler = CreateHandler(exampleRepo: mockExampleRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Sentence.Should().Be("새 문장");
            result.Data.Translation.Should().Be("New translation");
            mockExampleRepo.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.VocabularyExample>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Vocabulary Example",
                TestCaseID        = "Update_Vocabulary_Example_05",
                Description       = "Update Sentence + Translation to new valid values → updated successfully",
                ExpectedResult    = "Return 200, DTO.Sentence = '새 문장', UpdateAsync called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "New valid Sentence", "New valid Translation", "No duplicate", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Update_Vocabulary_Example_06 | N | Update Status only → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_UpdateStatusOnly_ShouldChangeStatusAndReturn200()
        {
            // Arrange
            var example = MockVocabularyExampleRepository.GetSampleExample();
            example.Status = VocabularyExampleStatus.Active;

            var command = new UpdateVocabularyExampleCommand
            {
                ExampleId  = example.ExampleId,
                UpdateData = new VocabularyExampleUpdateDto
                {
                    Status = VocabularyExampleStatus.Deleted
                }
            };

            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(existingById: example);
            var handler = CreateHandler(exampleRepo: mockExampleRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            example.Status.Should().Be(VocabularyExampleStatus.Deleted);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Vocabulary Example",
                TestCaseID        = "Update_Vocabulary_Example_06",
                Description       = "Update only Status = Deleted → status changed, returns 200",
                ExpectedResult    = "Return 200, example.Status = Deleted",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Only Status updated", "Active → Deleted", "Return 200" }
            });
        }
    }
}