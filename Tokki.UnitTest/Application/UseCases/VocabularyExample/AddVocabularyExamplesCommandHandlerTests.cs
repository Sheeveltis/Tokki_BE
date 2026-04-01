using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Application.UseCases.VocabularyExample.Commands.AddExamples;
using Tokki.Application.UseCases.VocabularyExample.DTOs;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.VocabularyExample
{
    public class AddVocabularyExamplesCommandHandlerTests
    {
        private AddVocabularyExamplesCommandHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<IVocabularyExampleRepository>? exampleRepo = null,
            bool unauthorized = false)
        {
            return new AddVocabularyExamplesCommandHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                (exampleRepo ?? MockVocabularyExampleRepository.GetMock()).Object,
                MockIdGeneratorService.GetMock().Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("USER-001").Object,
                new Mock<ILogger<AddVocabularyExamplesCommandHandler>>().Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-ADD-01 | A | No token → 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            // Arrange
            var command = new AddVocabularyExamplesCommand
            {
                VocabularyId = "VOCAB-001",
                Examples = new List<VocabularyExampleDto>
                {
                    new VocabularyExampleDto { Sentence = "테스트 문장." }
                }
            };
            var handler = CreateHandler(unauthorized: true);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Add", new TestCaseDetail
            {
                FunctionGroup     = "Add Vocabulary Examples",
                TestCaseID        = "TC-VEXM-ADD-01",
                Description       = "Add example sentences without authentication token",
                ExpectedResult    = "Return 401 Unauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No UserId in Claims", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-ADD-02 | A | Empty VocabularyId → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyVocabularyId_ShouldReturn400()
        {
            // Arrange
            var command = new AddVocabularyExamplesCommand
            {
                VocabularyId = "",
                Examples = new List<VocabularyExampleDto>
                {
                    new VocabularyExampleDto { Sentence = "테스트 문장." }
                }
            };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Add", new TestCaseDetail
            {
                FunctionGroup     = "Add Vocabulary Examples",
                TestCaseID        = "TC-VEXM-ADD-02",
                Description       = "Add examples with empty VocabularyId",
                ExpectedResult    = "Return 400 VocabularyIdEmpty",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyId = empty string", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-ADD-03 | A | Empty Examples list → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyExamplesList_ShouldReturn400()
        {
            // Arrange
            var command = new AddVocabularyExamplesCommand
            {
                VocabularyId = "VOCAB-001",
                Examples     = new List<VocabularyExampleDto>()
            };
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocab: MockVocabularyRepository.GetSampleVocabulary()));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Add", new TestCaseDetail
            {
                FunctionGroup     = "Add Vocabulary Examples",
                TestCaseID        = "TC-VEXM-ADD-03",
                Description       = "Add examples with empty Examples list",
                ExpectedResult    = "Return 400 ExamplesEmpty",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Examples = empty list", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-ADD-04 | A | VocabularyId not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabularyNotFound_ShouldReturn404()
        {
            // Arrange
            var command = new AddVocabularyExamplesCommand
            {
                VocabularyId = "VOCAB-INVALID",
                Examples = new List<VocabularyExampleDto>
                {
                    new VocabularyExampleDto { Sentence = "테스트 문장." }
                }
            };
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: null));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Add", new TestCaseDetail
            {
                FunctionGroup     = "Add Vocabulary Examples",
                TestCaseID        = "TC-VEXM-ADD-04",
                Description       = "Add examples with VocabularyId that doesn't exist",
                ExpectedResult    = "Return 404 VocabularyNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid VocabularyId", "Vocab = null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-ADD-05 | A | Duplicate sentence → skip → 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicateSentence_ShouldSkipAndReturn201()
        {
            // Arrange
            var command = new AddVocabularyExamplesCommand
            {
                VocabularyId = "VOCAB-001",
                Examples = new List<VocabularyExampleDto>
                {
                    new VocabularyExampleDto { Sentence = "안녕하세요, 만나서 반갑습니다.", Translation = "Nice to meet you." }
                }
            };
            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(
                existingExample: MockVocabularyExampleRepository.GetSampleExample());
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: MockVocabularyRepository.GetSampleVocabulary()),
                exampleRepo: mockExampleRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.CreatedExamples.Should().BeEmpty();
            result.Data.SkippedSentences.Should().HaveCount(1);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Add", new TestCaseDetail
            {
                FunctionGroup     = "Add Vocabulary Examples",
                TestCaseID        = "TC-VEXM-ADD-05",
                Description       = "Example sentence already exists (duplicate) → skipped, still returns 201",
                ExpectedResult    = "Return 201, CreatedExamples = empty, SkippedSentences.Count = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Sentence already exists in DB", "Skip duplicate", "Return 201" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-ADD-06 | N | Valid new example → created → 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidNewExample_ShouldCreateAndReturn201()
        {
            // Arrange
            var command = new AddVocabularyExamplesCommand
            {
                VocabularyId = "VOCAB-001",
                Examples = new List<VocabularyExampleDto>
                {
                    new VocabularyExampleDto { Sentence = "새로운 문장입니다.", Translation = "This is a new sentence." }
                }
            };
            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(existingExample: null);
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: MockVocabularyRepository.GetSampleVocabulary()),
                exampleRepo: mockExampleRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.CreatedExamples.Should().HaveCount(1);
            result.Data.SkippedSentences.Should().BeEmpty();
            mockExampleRepo.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.VocabularyExample>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Add", new TestCaseDetail
            {
                FunctionGroup     = "Add Vocabulary Examples",
                TestCaseID        = "TC-VEXM-ADD-06",
                Description       = "Add 1 valid new example sentence → created successfully",
                ExpectedResult    = "Return 201, CreatedExamples.Count = 1, AddAsync called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid VocabularyId", "New sentence (no duplicate)", "AddAsync called once", "Return 201" }
            });
        }
    }
}