using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockExamRepository
    {
        public static Mock<IExamRepository> GetMock(bool isTitleExists = false)
        {
            var mockRepo = new Mock<IExamRepository>();

            // FIX: Thay ch?"null" tr?n tr?i b?ng It.IsAny<string>() 
            mockRepo.Setup(x => x.IsTitleExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(isTitleExists);

            // Setup add and save
            mockRepo.Setup(x => x.AddAsync(It.IsAny<Exam>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

            return mockRepo;
        }
    }
}