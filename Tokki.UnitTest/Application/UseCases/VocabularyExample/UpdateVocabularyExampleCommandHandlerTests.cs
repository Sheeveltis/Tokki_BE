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
                    : MockHttpContextAccessor.GetMock("ADMIN-001").Object,
                new Mock<ILogger<UpdateVocabularyExampleCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_ExampleNotFound_ShouldReturn404()
        {
            var command = new UpdateVocabularyExampleCommand
            {
                ExampleId = "EX-INVALID",
                UpdateData = new VocabularyExampleUpdateDto
                {
                    Sentence = "Câu mới."
                }
            };

            var handler = CreateHandler(
                exampleRepo: MockVocabularyExampleRepository.GetMock(existingById: null));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("VocabExample - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vocabulary Example",
                TestCaseID = "TC-VEXM-UPD-01",
                Description = "Update câu ví dụ với ExampleId không tồn tại",
                ExpectedResult = "Return 404 ExampleNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid ExampleId",
                    "Example = null",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_DuplicateSentence_ShouldReturn400()
        {
            var existingExample = MockVocabularyExampleRepository.GetSampleExample("EX-001");

            // Câu mới trùng với câu của example khác (EX-003)
            var duplicateExample = MockVocabularyExampleRepository.GetSampleExample("EX-003");
            duplicateExample.Sentence = "Câu bị trùng.";

            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(
                existingById: existingExample);

            // GetBySentenceAsync trả về example khác (EX-003) → duplicate
            mockExampleRepo.Setup(x => x.GetBySentenceAsync(
                        It.IsAny<string>(),
                        "Câu bị trùng."))
                           .ReturnsAsync(duplicateExample);

            var command = new UpdateVocabularyExampleCommand
            {
                ExampleId = "EX-001",
                UpdateData = new VocabularyExampleUpdateDto
                {
                    Sentence = "Câu bị trùng."
                }
            };

            var handler = CreateHandler(exampleRepo: mockExampleRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("VocabExample - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vocabulary Example",
                TestCaseID = "TC-VEXM-UPD-02",
                Description = "Update Sentence thành câu đã tồn tại của example khác → duplicate",
                ExpectedResult = "Return 400 ExampleDuplicate",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "New Sentence trùng example khác (khác ExampleId)",
                    "Return 400 ExampleDuplicate"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidUpdate_ShouldUpdateSentenceAndReturn200()
        {
            var existingExample = MockVocabularyExampleRepository.GetSampleExample("EX-001");

            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(
                existingById: existingExample,
                existingExample: null); // GetBySentenceAsync trả về null → không trùng

            var command = new UpdateVocabularyExampleCommand
            {
                ExampleId = "EX-001",
                UpdateData = new VocabularyExampleUpdateDto
                {
                    Sentence = "업데이트된 문장입니다.",
                    Translation = "Câu đã được cập nhật."
                }
            };

            var handler = CreateHandler(exampleRepo: mockExampleRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Sentence.Should().Be("업데이트된 문장입니다.");
            result.Data.Translation.Should().Be("Câu đã được cập nhật.");

            mockExampleRepo.Verify(
                x => x.UpdateAsync(It.IsAny<Domain.Entities.VocabularyExample>()),
                Times.Once);

            QACollector.LogTestCase("VocabExample - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vocabulary Example",
                TestCaseID = "TC-VEXM-UPD-03",
                Description = "Update Sentence và Translation hợp lệ → cập nhật thành công",
                ExpectedResult = "Return 200, Sentence và Translation được cập nhật",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid ExampleId",
                    "New Sentence không trùng",
                    "UpdateAsync called once",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_NullSentenceInUpdateData_ShouldNotChangeSentence()
        {
            // Sentence = null → bỏ qua, không đổi
            var existingExample = MockVocabularyExampleRepository.GetSampleExample("EX-001");
            var originalSentence = existingExample.Sentence;

            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(
                existingById: existingExample);

            var command = new UpdateVocabularyExampleCommand
            {
                ExampleId = "EX-001",
                UpdateData = new VocabularyExampleUpdateDto
                {
                    Sentence = null, // null → bỏ qua
                    Translation = "Bản dịch mới."
                }
            };

            var handler = CreateHandler(exampleRepo: mockExampleRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Sentence không bị thay đổi
            existingExample.Sentence.Should().Be(originalSentence);
            result.Data.Translation.Should().Be("Bản dịch mới.");

            QACollector.LogTestCase("VocabExample - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vocabulary Example",
                TestCaseID = "TC-VEXM-UPD-04",
                Description = "UpdateData.Sentence = null → Sentence không thay đổi, chỉ Translation được cập nhật",
                ExpectedResult = "Return 200, Sentence giữ nguyên, Translation cập nhật",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Sentence = null (boundary: skip update)",
                    "Translation có giá trị mới",
                    "Sentence không đổi",
                    "Return 200"
                }
            });
        }
    }
}