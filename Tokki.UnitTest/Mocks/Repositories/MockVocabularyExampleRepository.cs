using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockVocabularyExampleRepository
    {
        public static Mock<IVocabularyExampleRepository> GetMock(
            VocabularyExample? existingExample = null)
        {
            var mockRepo = new Mock<IVocabularyExampleRepository>();

            // BeginTransactionAsync — trả về fake transaction
            var mockTransaction = new Mock<IDbContextTransaction>();
            mockTransaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);
            mockTransaction.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);
            mockTransaction.Setup(x => x.DisposeAsync())
                           .Returns(ValueTask.CompletedTask);

            mockRepo.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockTransaction.Object);

            // GetBySentenceAsync
            mockRepo.Setup(x => x.GetBySentenceAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .ReturnsAsync(existingExample);

            // AddAsync
            mockRepo.Setup(x => x.AddAsync(It.IsAny<VocabularyExample>()))
                    .Returns(Task.CompletedTask);

            // SaveChangesAsync
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(1);

            return mockRepo;
        }
    }
}