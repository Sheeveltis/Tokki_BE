using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.Queries.GetById;
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

        [Fact]
        public async Task Handle_VocabNotFound_ShouldReturn404()
        {
            var query = new GetVocabularyDetailByIdQuery { VocabularyId = "VOCAB-INVALID" };

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: null));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Vocabulary - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "Get Vocabulary Detail By Id",
                TestCaseID = "TC-VOCAB-GID-01",
                Description = "Lấy chi tiết vocab với ID không tồn tại",
                ExpectedResult = "Return 404 VocabularyNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid VocabularyId",
                    "Vocab = null",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_VocabIsDeleted_ShouldReturn404()
        {
            var query = new GetVocabularyDetailByIdQuery { VocabularyId = "VOCAB-004" };

            // Vocab tồn tại nhưng Status = Deleted → handler trả về 404
            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocab: MockVocabularyRepository.GetSampleVocabDeleted()));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Vocabulary - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "Get Vocabulary Detail By Id",
                TestCaseID = "TC-VOCAB-GID-02",
                Description = "Lấy chi tiết vocab đã bị xóa (Status = Deleted) → không hiển thị",
                ExpectedResult = "Return 404 VocabularyNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "VocabularyId hợp lệ",
                    "Status = Deleted",
                    "Return 404 (không expose vocab đã xóa)"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidActiveVocab_ShouldReturn200WithDto()
        {
            var query = new GetVocabularyDetailByIdQuery { VocabularyId = "VOCAB-001" };

            var vocab = MockVocabularyRepository.GetSampleVocabWithChildren(
                status: VocabularyStatus.Active);

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: vocab));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.VocabularyId.Should().Be("VOCAB-001");
            result.Data.Text.Should().NotBeNullOrEmpty();

            QACollector.LogTestCase("Vocabulary - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "Get Vocabulary Detail By Id",
                TestCaseID = "TC-VOCAB-GID-03",
                Description = "Lấy chi tiết vocab hợp lệ, Active → trả về DTO đầy đủ",
                ExpectedResult = "Return 200, Data.VocabularyId = VOCAB-001",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid VocabularyId",
                    "Status = Active",
                    "Has Topics and Examples",
                    "Return 200"
                }
            });
        }
    }
}