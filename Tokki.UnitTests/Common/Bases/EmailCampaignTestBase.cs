using Moq;
using Tokki.Application.IRepositories;

namespace Tokki.UnitTests.Common.Bases
{
    public abstract class EmailCampaignTestBase
    {
        protected readonly Mock<IEmailJobRepository> _mockRepo;

        protected EmailCampaignTestBase()
        {
            _mockRepo = new Mock<IEmailJobRepository>();
        }
    }
}
