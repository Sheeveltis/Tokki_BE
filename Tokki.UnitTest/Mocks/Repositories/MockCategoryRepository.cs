using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockCategoryRepository
    {
        public static Mock<ICategoryRepository> GetMock(List<Category>? predefinedCategories = null)
        {
            var mockRepo = new Mock<ICategoryRepository>();
            var categories = predefinedCategories ?? new List<Category>();

            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(categories);

            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, CancellationToken token) => 
                    categories.FirstOrDefault(c => c.Id == id));

            mockRepo.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, CancellationToken token) => 
                    categories.Any(c => c.Id == id));

            mockRepo.Setup(x => x.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.DeleteAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return mockRepo;
        }

        public static Category GetSampleCategory(string id = "CAT-001", string name = "Blogging Category") => new()
        {
            Id = id,
            Name = name,
            Blogs = new List<Blog>()
        };
    }
}
