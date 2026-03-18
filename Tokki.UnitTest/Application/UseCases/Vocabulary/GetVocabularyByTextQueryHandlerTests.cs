using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.Queries;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Vocabulary
{
    public class GetVocabularyByTextQueryHandlerTests
    {
        private GetVocabularyByTextQueryHandler CreateHandler(
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null)
        {
            return new GetVocabularyByTextQueryHandler(
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                (vocabTopicRepo ?? MockVocabularyTopicRepository.GetMock()).Object);
        }

        [Fact]
        public async Task Handle_EmptyText_ShouldReturn400()
        {
            var query = new GetVocabularyByTextQuery { Text = "" };

            var handler = CreateHandler();
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Vocabulary - Get By Text", new TestCaseDetail
            {
                FunctionGroup = "Get Vocabulary By Text",
                TestCaseID = "TC-VOCAB-GBT-01",
                Description = "Tìm vocab với Text rỗng",
                ExpectedResult = "Return 400 INVALID_INPUT",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Text = empty string",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_TextNotFound_ShouldReturn404()
        {
            var query = new GetVocabularyByTextQuery { Text = "없는단어" };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesByTextAsync(
         It.IsAny<string>(),
         It.IsAny<int>(),
         It.IsAny<int>(),
         It.IsAny<string?>(),
         It.IsAny<VocabularyStatus?>()))
     .ReturnsAsync((
         new List<Tokki.Domain.Entities.Vocabulary>(),
         0
     ));
            var handler = CreateHandler(vocabRepo: mockVocabRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Vocabulary - Get By Text", new TestCaseDetail
            {
                FunctionGroup = "Get Vocabulary By Text",
                TestCaseID = "TC-VOCAB-GBT-02",
                Description = "Tìm vocab với Text không có trong hệ thống → 404",
                ExpectedResult = "Return 404 VOCABULARY_NOT_FOUND",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Text không tồn tại trong DB",
                    "TotalCount = 0",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_TextWithMultipleDefinitions_ShouldReturnAll()
        {
            // "은행" có 2 nghĩa: ngân hàng và quả ngân hạnh
            var query = new GetVocabularyByTextQuery { Text = "은행" };

            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                new Tokki.Domain.Entities.Vocabulary
                {
                    VocabularyId = "VOCAB-001",
                    Text = "은행",
                    Definition = "ngân hàng",
                    Status = VocabularyStatus.Active,
                    VocabularyTopics = new List<VocabularyTopic>(),
                    VocabularyExamples = new List<Tokki.Domain.Entities.VocabularyExample>()
                },
                new Tokki.Domain.Entities.Vocabulary
                {
                    VocabularyId = "VOCAB-002",
                    Text = "은행",
                    Definition = "quả ngân hạnh",
                    Status = VocabularyStatus.Active,
                    VocabularyTopics = new List<VocabularyTopic>(),
                    VocabularyExamples = new List<Tokki.Domain.Entities.VocabularyExample>()
                }
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetPagedVocabulariesByTextAsync(
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string?>(),
                        It.IsAny<VocabularyStatus?>()))
                         .ReturnsAsync((vocabs, 2));

            var mockVocabTopicRepo = MockVocabularyTopicRepository.GetMock(
                returnedByVocabId: new List<VocabularyTopic>());

            var handler = CreateHandler(
                vocabRepo: mockVocabRepo,
                vocabTopicRepo: mockVocabTopicRepo);

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.TotalCount.Should().Be(2);
            result.Data.Items.Should().HaveCount(2);

            QACollector.LogTestCase("Vocabulary - Get By Text", new TestCaseDetail
            {
                FunctionGroup = "Get Vocabulary By Text",
                TestCaseID = "TC-VOCAB-GBT-03",
                Description = "Text '은행' có 2 nghĩa khác nhau → trả về cả 2",
                ExpectedResult = "Return 200, TotalCount = 2",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Text = '은행'",
                    "2 vocab với Definition khác nhau",
                    "Return 200, Items.Count = 2"
                }
            });
        }
    }
}