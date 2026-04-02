using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.VocabularyExample.Queries.GetByVocabularyId;
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

        [Fact]
        public async Task Handle_EmptyVocabularyId_ShouldReturn400()
        {
            var query = new GetVocabularyExamplesByVocabularyIdQuery
            {
                VocabularyId = ""
            };

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("VocabExample - Get", new TestCaseDetail
            {
                FunctionGroup = "Get Vocabulary Examples By VocabularyId",
                TestCaseID = "TC-VEXM-GET-01",
                Description = "Lấy danh sách câu ví dụ với VocabularyId rỗng",
                ExpectedResult = "Return 400 VOCAB_ID_EMPTY",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "VocabularyId = empty string",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_NoExamples_ShouldReturnEmptyList200()
        {
            var query = new GetVocabularyExamplesByVocabularyIdQuery
            {
                VocabularyId = "VOCAB-001"
            };

            var handler = CreateHandler(
                exampleRepo: MockVocabularyExampleRepository.GetMock(
                    returnedByVocabId: new List<Domain.Entities.VocabularyExample>()));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("VocabExample - Get", new TestCaseDetail
            {
                FunctionGroup = "Get Vocabulary Examples By VocabularyId",
                TestCaseID = "TC-VEXM-GET-02",
                Description = "Vocab không có câu ví dụ nào → trả về empty list",
                ExpectedResult = "Return 200, Data = empty list",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid VocabularyId",
                    "No examples in DB",
                    "Return 200 empty list"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidVocabularyId_ShouldReturnMappedDtoList()
        {
            var query = new GetVocabularyExamplesByVocabularyIdQuery
            {
                VocabularyId = "VOCAB-001"
            };

            var examples = MockVocabularyExampleRepository.GetSampleExampleList("VOCAB-001");

            var handler = CreateHandler(
                exampleRepo: MockVocabularyExampleRepository.GetMock(
                    returnedByVocabId: examples));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().HaveCount(2);
            result.Data[0].ExampleId.Should().Be("EX-001");
            result.Data[0].Sentence.Should().Be("안녕하세요.");
            result.Data[1].ExampleId.Should().Be("EX-002");

            QACollector.LogTestCase("VocabExample - Get", new TestCaseDetail
            {
                FunctionGroup = "Get Vocabulary Examples By VocabularyId",
                TestCaseID = "TC-VEXM-GET-03",
                Description = "Vocab có 2 câu ví dụ → trả về list 2 DTO được map đúng",
                ExpectedResult = "Return 200, Data.Count = 2, DTO map đúng ExampleId và Sentence",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid VocabularyId",
                    "2 examples in DB",
                    "DTO mapped correctly",
                    "Return 200"
                }
            });
        }
    }
}