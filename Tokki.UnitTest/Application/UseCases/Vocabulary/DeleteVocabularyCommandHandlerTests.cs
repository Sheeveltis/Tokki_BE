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

        [Fact]
        public async Task Handle_VocabNotFound_ShouldReturn404()
        {
            var command = new DeleteVocabularyCommand { VocabularyId = "VOCAB-INVALID" };

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(returnedVocabWithChildren: null));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Vocabulary - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Vocabulary",
                TestCaseID = "TC-VOCAB-DEL-01",
                Description = "Xóa vocabulary với ID không tồn tại",
                ExpectedResult = "Return 404 VocabularyNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid VocabularyId", "Return 404" }
            });
        }

        [Fact]
        public async Task Handle_VocabAlreadyDeleted_ShouldReturn400()
        {
            var command = new DeleteVocabularyCommand { VocabularyId = "VOCAB-004" };

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocabWithChildren: MockVocabularyRepository.GetSampleVocabDeleted()));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Vocabulary - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Vocabulary",
                TestCaseID = "TC-VOCAB-DEL-02",
                Description = "Xóa vocabulary đã bị xóa trước đó (Status = Deleted)",
                ExpectedResult = "Return 400 VocabularyAlreadyDeleted",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Deleted", "Return 400" }
            });
        }

        [Fact]
        public async Task Handle_ValidVocab_ShouldSoftDeleteCascadeAndReturn200()
        {
            var command = new DeleteVocabularyCommand { VocabularyId = "VOCAB-001" };

            var vocabWithChildren = MockVocabularyRepository.GetSampleVocabWithChildren();

            var handler = CreateHandler(
                vocabRepo: MockVocabularyRepository.GetMock(
                    returnedVocabWithChildren: vocabWithChildren));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            vocabWithChildren.Status.Should().Be(VocabularyStatus.Deleted);
            vocabWithChildren.VocabularyTopics.Should()
                .OnlyContain(vt => vt.Status == VocabularyTopicStatus.Deleted);
            vocabWithChildren.VocabularyExamples.Should()
                .OnlyContain(ex => ex.Status == VocabularyExampleStatus.Deleted);

            QACollector.LogTestCase("Vocabulary - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Vocabulary",
                TestCaseID = "TC-VOCAB-DEL-03",
                Description = "Xóa vocab hợp lệ → soft delete cascade xuống Topics và Examples",
                ExpectedResult = "Vocab + Topics + Examples đều Status = Deleted, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid VocabularyId",
                    "Has Topics and Examples",
                    "Cascade soft delete",
                    "Return 200"
                }
            });
        }
    }
}