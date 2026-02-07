using Moq;
using Tokki.Application.IRepositories;

namespace Tokki.UnitTests.Common.Bases
{
    public abstract class SystemConfigTestBase
    {
        protected readonly Mock<ISystemConfigRepository> _mockRepo;

        protected SystemConfigTestBase()
        {
            _mockRepo = new Mock<ISystemConfigRepository>(MockBehavior.Loose);
        }
    }
}
