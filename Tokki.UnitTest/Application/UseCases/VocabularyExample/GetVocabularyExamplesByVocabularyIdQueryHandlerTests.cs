using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.VocabularyExample.Queries.GetByVocabularyId;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.VocabularyExample
{
    public class GetVocabularyExamplesByVocabularyIdQueryHandlerTests
    {
        private GetVocabularyExamplesByVocabularyIdQueryHandler CreateHandler(
            Mock<IVocabularyExampleRepository>? exampleRepo = null)
        {
            return new GetVocabularyExamplesByVocabularyIdQueryHandler(
                (exampleRepo ?? MockVocabularyExampleRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-GBV-01 | A | Empty VocabularyId → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyVocabularyId_ShouldReturn400()
        {
            // Arrange
            var query = new GetVocabularyExamplesByVocabularyIdQuery { VocabularyId = "" };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Get By Vocab", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Examples By VocabularyId",
                TestCaseID        = "TC-VEXM-GBV-01",
                Description       = "Get examples with empty VocabularyId",
                ExpectedResult    = "Return 400 VocabularyId empty",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyId = empty string", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-GBV-02 | A | Whitespace VocabularyId → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WhitespaceVocabularyId_ShouldReturn400()
        {
            // Arrange
            var query = new GetVocabularyExamplesByVocabularyIdQuery { VocabularyId = "   " };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Get By Vocab", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Examples By VocabularyId",
                TestCaseID        = "TC-VEXM-GBV-02",
                Description       = "Get examples with whitespace-only VocabularyId",
                ExpectedResult    = "Return 400 VocabularyId empty",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VocabularyId = whitespace only", "IsNullOrWhiteSpace check", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-GBV-03 | N | Valid VocabularyId, no examples → 200 empty list
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoExamples_ShouldReturn200EmptyList()
        {
            // Arrange
            var query = new GetVocabularyExamplesByVocabularyIdQuery { VocabularyId = "VOCAB-001" };
            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(
                returnedByVocabId: new List<Domain.Entities.VocabularyExample>());
            var handler = CreateHandler(exampleRepo: mockExampleRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeEmpty();

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Get By Vocab", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Examples By VocabularyId",
                TestCaseID        = "TC-VEXM-GBV-03",
                Description       = "Valid VocabularyId but vocab has no example sentences → 200 empty list",
                ExpectedResult    = "Return 200, Data = empty list",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid VocabularyId", "No examples in DB", "Return 200 empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-GBV-04 | N | Valid VocabularyId, has 2 active examples → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_HasExamples_ShouldReturn200WithList()
        {
            // Arrange
            var query = new GetVocabularyExamplesByVocabularyIdQuery { VocabularyId = "VOCAB-001" };
            var examples = new List<Domain.Entities.VocabularyExample>
            {
                new Domain.Entities.VocabularyExample { ExampleId = "EX-001", VocabularyId = "VOCAB-001", Sentence = "안녕하세요!", Translation = "Hello!", Status = Tokki.Domain.Enums.VocabularyExampleStatus.Active },
                new Domain.Entities.VocabularyExample { ExampleId = "EX-002", VocabularyId = "VOCAB-001", Sentence = "감사합니다.", Translation = "Thank you.", Status = Tokki.Domain.Enums.VocabularyExampleStatus.Active }
            };
            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(returnedByVocabId: examples);
            var handler = CreateHandler(exampleRepo: mockExampleRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().HaveCount(2);
            result.Data[0].Sentence.Should().Be("안녕하세요!");

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Get By Vocab", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Examples By VocabularyId",
                TestCaseID        = "TC-VEXM-GBV-04",
                Description       = "Valid VocabularyId with 2 active examples → returns list of 2",
                ExpectedResult    = "Return 200, Data.Count = 2",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid VocabularyId", "2 active examples", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-GBV-05 | N | DTO correctly maps Sentence and Translation fields
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidExamples_DtoShouldMapSentenceAndTranslation()
        {
            // Arrange
            var query = new GetVocabularyExamplesByVocabularyIdQuery { VocabularyId = "VOCAB-001" };
            var examples = new List<Domain.Entities.VocabularyExample>
            {
                new Domain.Entities.VocabularyExample { ExampleId = "EX-001", VocabularyId = "VOCAB-001", Sentence = "좋아요.", Translation = "Good.", Status = Tokki.Domain.Enums.VocabularyExampleStatus.Active }
            };
            var handler = CreateHandler(
                exampleRepo: MockVocabularyExampleRepository.GetMock(returnedByVocabId: examples));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data[0].ExampleId.Should().Be("EX-001");
            result.Data[0].Sentence.Should().Be("좋아요.");
            result.Data[0].Translation.Should().Be("Good.");

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Get By Vocab", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Examples By VocabularyId",
                TestCaseID        = "TC-VEXM-GBV-05",
                Description       = "Response DTO correctly maps ExampleId, Sentence, Translation from entity",
                ExpectedResult    = "Return 200, DTO fields correctly mapped",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "1 example", "DTO mapping verified", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-GBV-06 | N | Repository called exactly once
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidQuery_ShouldCallRepositoryOnce()
        {
            // Arrange
            var query = new GetVocabularyExamplesByVocabularyIdQuery { VocabularyId = "VOCAB-001" };
            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(
                returnedByVocabId: new List<Domain.Entities.VocabularyExample>());
            var handler = CreateHandler(exampleRepo: mockExampleRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockExampleRepo.Verify(
                x => x.GetByVocabularyIdAsync(It.IsAny<string>()),
                Times.Once);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Get By Vocab", new TestCaseDetail
            {
                FunctionGroup     = "Get Vocabulary Examples By VocabularyId",
                TestCaseID        = "TC-VEXM-GBV-06",
                Description       = "Handler calls GetByVocabularyIdAsync exactly once per request",
                ExpectedResult    = "Return 200, GetByVocabularyIdAsync called exactly once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid VocabularyId", "Repository called once", "Return 200" }
            });
        }
    }
}