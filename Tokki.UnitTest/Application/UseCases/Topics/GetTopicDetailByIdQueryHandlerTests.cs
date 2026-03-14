using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Queries.GetById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class GetTopicDetailByIdQueryHandlerTests
    {
        private GetTopicDetailByIdQueryHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null)
        {
            return new GetTopicDetailByIdQueryHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                (vocabTopicRepo ?? MockVocabularyTopicRepository.GetMock()).Object);
        }

        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var query = new GetTopicDetailByIdQuery { TopicId = "TOPIC-INVALID" };

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: null));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Topic - Get Detail By Id", new TestCaseDetail
            {
                FunctionGroup = "Get Topic Detail By Id",
                TestCaseID = "TC-TOPIC-GID-01",
                Description = "Lấy chi tiết topic với ID không tồn tại",
                ExpectedResult = "Return 404 TopicNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid TopicId",
                    "Topic = null",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidTopic_ShouldReturnOnlyActiveVocabMappings()
        {
            var query = new GetTopicDetailByIdQuery { TopicId = "TOPIC-001" };

            var topic = MockTopicRepository.GetSampleTopic();

            // 2 mapping: 1 Active, 1 Deleted → chỉ trả về 1
            var mappings = new List<VocabularyTopic>
            {
                new VocabularyTopic
                {
                    VocabularyId = "VOCAB-001",
                    TopicId = "TOPIC-001",
                    Status = VocabularyTopicStatus.Active,
                    Vocabulary = new Tokki.Domain.Entities.Vocabulary
                    {
                        VocabularyId = "VOCAB-001",
                        Text = "안녕",
                        Definition = "Xin chào",
                        Status = VocabularyStatus.Active
                    }
                },
                new VocabularyTopic
                {
                    VocabularyId = "VOCAB-002",
                    TopicId = "TOPIC-001",
                    Status = VocabularyTopicStatus.Deleted, // bị loại
                    Vocabulary = new Tokki.Domain.Entities.Vocabulary
                    {
                        VocabularyId = "VOCAB-002",
                        Text = "감사",
                        Status = VocabularyStatus.Active
                    }
                }
            };

            var mockVocabTopicRepo = MockVocabularyTopicRepository.GetMock(
                returnedByTopicId: mappings);

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: topic),
                vocabTopicRepo: mockVocabTopicRepo);

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.TopicId.Should().Be("TOPIC-001");
            result.Data.VocabularyCount.Should().Be(1);
            result.Data.Vocabularies.Should().HaveCount(1);
            result.Data.Vocabularies[0].VocabularyId.Should().Be("VOCAB-001");

            QACollector.LogTestCase("Topic - Get Detail By Id", new TestCaseDetail
            {
                FunctionGroup = "Get Topic Detail By Id",
                TestCaseID = "TC-TOPIC-GID-02",
                Description = "Topic có 2 mapping (1 Active, 1 Deleted) → chỉ trả về 1 vocab Active",
                ExpectedResult = "Return 200, VocabularyCount = 1, chỉ chứa VOCAB-001",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "2 VocabularyTopic mappings",
                    "1 Active, 1 Deleted",
                    "Filter chỉ Active",
                    "Return 200, VocabularyCount = 1"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidTopic_ShouldMapAuditFieldsCorrectly()
        {
            var query = new GetTopicDetailByIdQuery { TopicId = "TOPIC-001" };

            var topic = MockTopicRepository.GetSampleTopic();
            topic.ApprovedBy = "ADMIN-001";
            topic.ApprovedDate = DateTime.UtcNow.AddDays(-1);

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: topic),
                vocabTopicRepo: MockVocabularyTopicRepository.GetMock(
                    returnedByTopicId: new List<VocabularyTopic>()));

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.ApprovedBy.Should().Be("ADMIN-001");
            result.Data.ApprovedDate.Should().NotBeNull();
            result.Data.OrderIndex.Should().Be(topic.OrderIndex);

            QACollector.LogTestCase("Topic - Get Detail By Id", new TestCaseDetail
            {
                FunctionGroup = "Get Topic Detail By Id",
                TestCaseID = "TC-TOPIC-GID-03",
                Description = "Topic có ApprovedBy và ApprovedDate → DTO map đúng các audit fields",
                ExpectedResult = "Return 200, ApprovedBy = ADMIN-001, ApprovedDate != null",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Topic có ApprovedBy và ApprovedDate",
                    "DTO map đầy đủ audit fields",
                    "Return 200"
                }
            });
        }
    }
}