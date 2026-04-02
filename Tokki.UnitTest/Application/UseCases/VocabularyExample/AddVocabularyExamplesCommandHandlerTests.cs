using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.VocabularyExample.Commands.AddExamples;
using Tokki.Application.UseCases.VocabularyExample.DTOs;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;
using Tokki.Application.IRepositories;

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

        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            var command = new AddVocabularyExamplesCommand
            {
                VocabularyId = "VOCAB-001",
                Examples = new List<VocabularyExampleDto>
                {
                    new VocabularyExampleDto { Sentence = "테스트 문장." }
                }
            };

            var handler = CreateHandler(unauthorized: true);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("VocabExample - Add", new TestCaseDetail
            {
                FunctionGroup = "Add Vocabulary Examples",
                TestCaseID = "TC-VEXM-ADD-01",
                Description = "Thêm câu ví dụ khi không có token xác thực",
                ExpectedResult = "Return 401 Unauthorized",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "No UserId in Claims",
                    "Return 401"
                }
            });
        }

        [Fact]
        public async Task Handle_VocabularyNotFound_ShouldReturn404()
        {
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

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("VocabExample - Add", new TestCaseDetail
            {
                FunctionGroup = "Add Vocabulary Examples",
                TestCaseID = "TC-VEXM-ADD-02",
                Description = "Thêm câu ví dụ với VocabularyId không tồn tại",
                ExpectedResult = "Return 404 VocabularyNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid VocabularyId",
                    "Vocabulary = null",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_EmptyExamplesList_ShouldReturn400()
        {
            var command = new AddVocabularyExamplesCommand
            {
                VocabularyId = "VOCAB-001",
                Examples = new List<VocabularyExampleDto>() // rỗng
            };

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocab: MockVocabularyRepository.GetSampleVocabulary()));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("VocabExample - Add", new TestCaseDetail
            {
                FunctionGroup = "Add Vocabulary Examples",
                TestCaseID = "TC-VEXM-ADD-03",
                Description = "Thêm câu ví dụ với danh sách Examples rỗng",
                ExpectedResult = "Return 400 ExamplesEmpty",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Examples = empty list",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_DuplicateSentence_ShouldSkipAndReturn201()
        {
            // Câu đã tồn tại → bị skip, vẫn return 201
            var command = new AddVocabularyExamplesCommand
            {
                VocabularyId = "VOCAB-001",
                Examples = new List<VocabularyExampleDto>
                {
                    new VocabularyExampleDto
                    {
                        Sentence = "안녕하세요, 만나서 반갑습니다.",
                        Translation = "Trùng câu"
                    }
                }
            };

            // GetBySentenceAsync trả về example đã tồn tại → skip
            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(
                existingExample: MockVocabularyExampleRepository.GetSampleExample());

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocab: MockVocabularyRepository.GetSampleVocabulary()),
                exampleRepo: mockExampleRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.CreatedExamples.Should().BeEmpty();
            result.Data.SkippedSentences.Should().HaveCount(1);
            result.Message.Should().Contain("Bỏ qua trùng lặp");

            QACollector.LogTestCase("VocabExample - Add", new TestCaseDetail
            {
                FunctionGroup = "Add Vocabulary Examples",
                TestCaseID = "TC-VEXM-ADD-04",
                Description = "Câu ví dụ đã tồn tại → bị skip, vẫn return 201",
                ExpectedResult = "Return 201, CreatedExamples = empty, SkippedSentences = 1",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Sentence đã tồn tại trong DB",
                    "Skip duplicate",
                    "Return 201"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidNewExample_ShouldCreateAndReturn201()
        {
            var command = new AddVocabularyExamplesCommand
            {
                VocabularyId = "VOCAB-001",
                Examples = new List<VocabularyExampleDto>
                {
                    new VocabularyExampleDto
                    {
                        Sentence = "새로운 문장입니다.",
                        Translation = "Đây là câu mới."
                    }
                }
            };

            // GetBySentenceAsync trả về null → câu mới, không trùng
            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(
                existingExample: null);

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocab: MockVocabularyRepository.GetSampleVocabulary()),
                exampleRepo: mockExampleRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.CreatedExamples.Should().HaveCount(1);
            result.Data.SkippedSentences.Should().BeEmpty();

            // Verify AddAsync được gọi 1 lần
            mockExampleRepo.Verify(
                x => x.AddAsync(It.IsAny<Domain.Entities.VocabularyExample>()),
                Times.Once);

            QACollector.LogTestCase("VocabExample - Add", new TestCaseDetail
            {
                FunctionGroup = "Add Vocabulary Examples",
                TestCaseID = "TC-VEXM-ADD-05",
                Description = "Thêm 1 câu ví dụ mới hợp lệ → tạo thành công",
                ExpectedResult = "Return 201, CreatedExamples.Count = 1, AddAsync called once",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid VocabularyId",
                    "New sentence (no duplicate)",
                    "AddAsync called once",
                    "Return 201"
                }
            });
        }
    }
}