using Microsoft.Extensions.Logging;
using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;

namespace Tokki.UnitTests.Common.Bases
{
    public class BlogTestBase
    {
        protected readonly Mock<IBlogRepository> _mockRepo;
        protected readonly Mock<IIdGeneratorService> _mockIdGen;
        protected readonly Mock<ILogger<CreateBlogCommandHandler>> _mockLogger;
        protected readonly CreateBlogCommandHandler _handler;

        public BlogTestBase()
        {
            _mockRepo = new Mock<IBlogRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();
            _mockLogger = new Mock<ILogger<CreateBlogCommandHandler>>();

            _handler = new CreateBlogCommandHandler(
                _mockRepo.Object,
                _mockIdGen.Object,
                _mockLogger.Object
            );
        }
    }
}