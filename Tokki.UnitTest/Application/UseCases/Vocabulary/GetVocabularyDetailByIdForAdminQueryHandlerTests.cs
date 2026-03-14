using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.Queries.GetByIdForUser;
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

        [Fact]
        public async Task Handle_VocabNotFound_ShouldReturn404()
        {
            var query = new GetVocabularyDetailByIdForAdminQuery
            {
                VocabularyId = "VOCAB-INVALID"
            };

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: null));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Vocabulary - Get By Id For Admin", new TestCaseDetail
            {
                FunctionGroup = "Get Vocabulary Detail For Admin",
                TestCaseID = "TC-VOCAB-ADM-01",
                Description = "Admin lấy chi tiết vocab với ID không tồn tại",
                ExpectedResult = "Return 404 VocabularyNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid VocabularyId",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_DeletedVocab_ShouldStillReturn200ForAdmin()
        {
            // Admin query KHÔNG check Status = Deleted → vẫn trả về dữ liệu
            var query = new GetVocabularyDetailByIdForAdminQuery
            {
                VocabularyId = "VOCAB-004"
            };

            var deletedVocab = MockVocabularyRepository.GetSampleVocabDeleted();

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: deletedVocab));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Status.Should().Be(VocabularyStatus.Deleted);

            QACollector.LogTestCase("Vocabulary - Get By Id For Admin", new TestCaseDetail
            {
                FunctionGroup = "Get Vocabulary Detail For Admin",
                TestCaseID = "TC-VOCAB-ADM-02",
                Description = "Admin lấy vocab đã Deleted → vẫn trả về (Admin có quyền xem mọi trạng thái)",
                ExpectedResult = "Return 200, Data.Status = Deleted",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid VocabularyId",
                    "Status = Deleted",
                    "Admin query không filter Deleted",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidVocab_ShouldReturnFullAuditInfoDto()
        {
            var query = new GetVocabularyDetailByIdForAdminQuery
            {
                VocabularyId = "VOCAB-001"
            };

            var vocab = MockVocabularyRepository.GetSampleVocabWithChildren(
                status: VocabularyStatus.Active);

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocab: vocab));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.VocabularyId.Should().Be("VOCAB-001");
            result.Data.CreateBy.Should().NotBeNullOrEmpty();

            QACollector.LogTestCase("Vocabulary - Get By Id For Admin", new TestCaseDetail
            {
                FunctionGroup = "Get Vocabulary Detail For Admin",
                TestCaseID = "TC-VOCAB-ADM-03",
                Description = "Admin lấy vocab hợp lệ → DTO có đầy đủ audit fields (CreateBy, UpdateBy...)",
                ExpectedResult = "Return 200, CreateBy không null",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid VocabularyId",
                    "Status = Active",
                    "DTO includes audit fields",
                    "Return 200"
                }
            });
        }
    }
}