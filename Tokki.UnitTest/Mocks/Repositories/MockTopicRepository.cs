using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockTopicRepository
    {
        public static Mock<ITopicRepository> GetMock(
            Topic? returnedTopic = null,
            bool isTopicNameExists = false,
            int maxOrderIndex = 0)
        {
            var mockRepo = new Mock<ITopicRepository>();

            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(returnedTopic);

            mockRepo.Setup(x => x.IsTopicNameExistsAsync(
         It.IsAny<string>(),
         It.IsAny<string?>()))
     .ReturnsAsync(isTopicNameExists);

            // Overload với excludeId
            mockRepo.Setup(x => x.IsTopicNameExistsAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .ReturnsAsync(isTopicNameExists);

            mockRepo.Setup(x => x.AddAsync(It.IsAny<Topic>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<Topic>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.GetMaxOrderIndexAsync())
                    .ReturnsAsync(maxOrderIndex);

            mockRepo.Setup(x => x.GetMaxOrderIndexForVocabAsync())
                    .ReturnsAsync(maxOrderIndex);

            mockRepo.Setup(x => x.DecrementOrderIndexAfterAsync(
                        It.IsAny<int>(),
                        It.IsAny<TopicType>(),
                        It.IsAny<string>(),
                        It.IsAny<DateTime>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.ShiftOrderIndexUpFromAsync(
                        It.IsAny<int>(),
                        It.IsAny<TopicType>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<DateTime>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.ShiftOrderIndexBetweenAsync(
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<TopicType>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<DateTime>()))
                    .Returns(Task.CompletedTask);

            return mockRepo;
        }

        // ===== Sample Data =====

        public static Topic GetSampleTopic(
            string topicId = "TOPIC-001",
            TopicStatus status = TopicStatus.Active)
        {
            return new Topic
            {
                TopicId = topicId,
                TopicName = "Basic greetings",
                Description = "Common greetings",
                Level = (int)TopicLevel.Level1,
                Status = status,
                CreateBy = "STAFF-001",
                TopicType = TopicType.VocabStudy,
                OrderIndex = 1
            };
        }

        public static Topic GetSampleTopicPendingApproval(string topicId = "TOPIC-002")
        {
            return new Topic
            {
                TopicId = topicId,
                TopicName = "Family",
                Level = (int)TopicLevel.Level1,
                Status = TopicStatus.PendingApproval,
                CreateBy = "STAFF-001",
                TopicType = TopicType.VocabStudy
            };
        }

        public static Topic GetSampleTopicDraft(string topicId = "TOPIC-003")
        {
            return new Topic
            {
                TopicId = topicId,
                TopicName = "Job",
                Level = (int)TopicLevel.Level3,
                Status = TopicStatus.Draft,
                CreateBy = "STAFF-001",
                TopicType = TopicType.VocabStudy
            };
        }

        public static Topic GetSampleTopicDeleted(string topicId = "TOPIC-004")
        {
            return new Topic
            {
                TopicId = topicId,
                TopicName = "Topic has been deleted",
                Level = (int)TopicLevel.Level1,
                Status = TopicStatus.Deleted,
                CreateBy = "STAFF-001",
                TopicType = TopicType.VocabStudy
            };
        }
    }
}