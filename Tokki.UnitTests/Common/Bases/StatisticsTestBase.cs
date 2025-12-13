using Moq;
using Tokki.Application.IRepositories;

namespace Tokki.UnitTests.Common.Bases
{
    public class StatisticsTestBase
    {
        protected readonly Mock<IStatisticsRepository> _mockStatsRepo;

        public StatisticsTestBase()
        {
            _mockStatsRepo = new Mock<IStatisticsRepository>();
        }
    }
}