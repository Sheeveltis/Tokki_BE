using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockTemplatePartRepository
    {
        public static Mock<ITemplatePartRepository> GetMock(List<TemplatePart>? returnedParts = null)
        {
            var mockRepo = new Mock<ITemplatePartRepository>();

            mockRepo.Setup(x => x.GetByExamTemplateIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(returnedParts ?? new List<TemplatePart>());

            return mockRepo;
        }
    }
}