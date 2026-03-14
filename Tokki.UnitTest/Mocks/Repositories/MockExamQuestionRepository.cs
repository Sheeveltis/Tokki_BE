using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockExamQuestionRepository
    {
        public static Mock<IExamQuestionRepository> GetMock(ExamQuestion? returnedSlot = null)
        {
            var mockRepo = new Mock<IExamQuestionRepository>();

            // Setup mock for finding the slot in the exam
            mockRepo.Setup(x => x.GetByExamAndQuestionNoAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(returnedSlot);

            // Setup mock for Update
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<ExamQuestion>()))
                    .Returns(Task.CompletedTask);

            // Setup mock for SaveChanges
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

            return mockRepo;
        }
    }
}