using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockVocabularyTopicRepository
    {
        public static Mock<IVocabularyTopicRepository> GetMock(
            List<VocabularyTopic>? returnedByTopicId = null,
            List<VocabularyTopic>? returnedByVocabId = null)
        {
            var mockRepo = new Mock<IVocabularyTopicRepository>();

            // GetByTopicIdAsync — dùng cho FlashCard, GetVocabulariesByTopic
            mockRepo.Setup(x => x.GetByTopicIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(returnedByTopicId ?? new List<VocabularyTopic>());

            // GetByVocabularyIdAsync — dùng cho GetAllForManager, GetByText
            mockRepo.Setup(x => x.GetByVocabularyIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(returnedByVocabId ?? new List<VocabularyTopic>());

            return mockRepo;
        }

        public static List<VocabularyTopic> GetSampleActiveTopicMappings(
            string vocabId = "VOCAB-001",
            string topicId = "TOPIC-001")
        {
            return new List<VocabularyTopic>
            {
                new VocabularyTopic
                {
                    VocabularyId = vocabId,
                    TopicId = topicId,
                    Status = VocabularyTopicStatus.Active
                }
            };
        }
    }
}