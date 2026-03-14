using Moq;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockExamTemplateRepository
    {
        public static Mock<IExamTemplateRepository> GetMock(ExamTemplate? returnedTemplate = null)
        {
            var mockRepo = new Mock<IExamTemplateRepository>();

            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedTemplate);

            return mockRepo;
        }
    }
}