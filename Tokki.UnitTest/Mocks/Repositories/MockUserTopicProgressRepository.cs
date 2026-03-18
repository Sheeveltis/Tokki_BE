using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockUserTopicProgressRepository
    {
        public static Mock<IUserTopicProgressRepository> GetMock(
            UserTopicProgress? returnedProgress = null)
        {
            var mockRepo = new Mock<IUserTopicProgressRepository>();

            mockRepo.Setup(x => x.GetByUserIdAndTopicIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .ReturnsAsync(returnedProgress);

            mockRepo.Setup(x => x.AddAsync(It.IsAny<UserTopicProgress>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.Update(It.IsAny<UserTopicProgress>()));

            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            return mockRepo;
        }

        public static UserTopicProgress GetSampleProgress(
            string userId = "USER-001",
            string topicId = "TOPIC-001",
            bool isLearned = false)
        {
            return new UserTopicProgress
            {
                UserTopicProgressId = "PROG-001",
                UserId = userId,
                TopicId = topicId,
                IsLearned = isLearned,
                CompletedAt = null,
                LastActivityAt = System.DateTime.UtcNow
            };
        }
    }
}