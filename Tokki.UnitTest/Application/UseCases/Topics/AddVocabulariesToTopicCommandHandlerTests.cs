using FluentAssertions;
using FluentValidation;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.Commands.AddVocabulariesToTopic;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Topics
{
    public class AddVocabulariesToTopicCommandHandlerTests
    {
        private AddVocabulariesToTopicCommandHandler CreateHandler(
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null)
        {
            return new AddVocabulariesToTopicCommandHandler(
                (topicRepo ?? MockTopicRepository.GetMock()).Object,
                (vocabRepo ?? MockVocabularyRepository.GetMock()).Object,
                (vocabTopicRepo ?? MockVocabularyTopicRepository.GetMock()).Object,
                MockHttpContextAccessor.GetMock("ADMIN-001").Object,
                new AddVocabulariesToTopicCommandValidator());
        }

        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            var command = new AddVocabulariesToTopicCommand
            {
                TopicId = "TOPIC-INVALID",
                VocabularyIds = new List<string> { "VOCAB-001" }
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>()))
                         .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary>
                         {
                             MockVocabularyRepository.GetSampleVocabulary("VOCAB-001")
                         });

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(returnedTopic: null),
                vocabRepo: mockVocabRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Topic - Add Vocabularies", new TestCaseDetail
            {
                FunctionGroup = "Add Vocabularies To Topic",
                TestCaseID = "TC-TOPIC-AVT-01",
                Description = "Thêm vocab vào topic với TopicId không tồn tại",
                ExpectedResult = "Return 404 TopicNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid TopicId", "Return 404" }
            });
        }

        [Fact]
        public async Task Handle_NoValidVocabFound_ShouldReturn400()
        {
            var command = new AddVocabulariesToTopicCommand
            {
                TopicId = "TOPIC-001",
                VocabularyIds = new List<string> { "VOCAB-INVALID" }
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            // GetByIdsAsync trả về empty → không tìm thấy vocab nào
            mockVocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>()))
                         .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary>());

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopic()),
                vocabRepo: mockVocabRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Topic - Add Vocabularies", new TestCaseDetail
            {
                FunctionGroup = "Add Vocabularies To Topic",
                TestCaseID = "TC-TOPIC-AVT-02",
                Description = "Thêm vocab với toàn bộ VocabularyId không tồn tại",
                ExpectedResult = "Return 400 NoValidVocabulariesFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "All VocabularyIds invalid",
                    "GetByIdsAsync returns empty",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidData_ShouldReturn200WithAddedCount()
        {
            var command = new AddVocabulariesToTopicCommand
            {
                TopicId = "TOPIC-001",
                VocabularyIds = new List<string> { "VOCAB-001", "VOCAB-002" }
            };

            var vocabs = new List<Tokki.Domain.Entities.Vocabulary>
            {
                MockVocabularyRepository.GetSampleVocabulary("VOCAB-001"),
                MockVocabularyRepository.GetSampleVocabulary("VOCAB-002")
            };

            var mockVocabRepo = MockVocabularyRepository.GetMock();
            mockVocabRepo.Setup(x => x.GetByIdsAsync(It.IsAny<List<string>>()))
                         .ReturnsAsync(vocabs);

            var mockVocabTopicRepo = MockVocabularyTopicRepository.GetMock();
            mockVocabTopicRepo.Setup(x => x.AddOrReactivateVocabulariesToTopicAsync(
                        It.IsAny<string>(),
                        It.IsAny<List<Tokki.Domain.Entities.Vocabulary>>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                     .ReturnsAsync((2, 0, new List<string>()));

            var handler = CreateHandler(
                topicRepo: MockTopicRepository.GetMock(
                    returnedTopic: MockTopicRepository.GetSampleTopic()),
                vocabRepo: mockVocabRepo,
                vocabTopicRepo: mockVocabTopicRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(2);

            QACollector.LogTestCase("Topic - Add Vocabularies", new TestCaseDetail
            {
                FunctionGroup = "Add Vocabularies To Topic",
                TestCaseID = "TC-TOPIC-AVT-03",
                Description = "Thêm 2 vocab hợp lệ vào topic → addedOrReactivated = 2",
                ExpectedResult = "Return 200, Data = 2",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "2 valid VocabularyIds",
                    "No existing mappings",
                    "AddOrReactivate returns (2, 0, [])",
                    "Return 200"
                }
            });
        }
    }
}