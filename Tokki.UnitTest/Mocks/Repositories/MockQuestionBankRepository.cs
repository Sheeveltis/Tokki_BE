using Moq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockQuestionBankRepository
    {
        public static Mock<IQuestionBankRepository> GetMock(QuestionBank? returnedQuestion = null)
        {
            var mockRepo = new Mock<IQuestionBankRepository>();

            // Setup mock for GetByIdAsync
            // If returnedQuestion is provided, it returns that object. Otherwise, it returns null.
            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(returnedQuestion);

            return mockRepo;
        }
    }
}