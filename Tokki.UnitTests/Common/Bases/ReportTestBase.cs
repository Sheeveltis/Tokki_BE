using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;

namespace Tokki.UnitTests.Common.Bases
{
    public class ReportTestBase
    {
        protected readonly Mock<IReportRepository> _mockReportRepo;
        protected readonly Mock<IIdGeneratorService> _mockIdGen;

        public ReportTestBase()
        {
            _mockReportRepo = new Mock<IReportRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();

            _mockIdGen.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("REPORT_MOCK_ID");
        }
    }
}