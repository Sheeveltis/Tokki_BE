using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;

namespace Tokki.UnitTests.Common.Bases
{
    public class VipPackageTestBase
    {
        protected readonly Mock<IVipPackageRepository> _mockVipRepo;
        protected readonly Mock<IIdGeneratorService> _mockIdGen;

        public VipPackageTestBase()
        {
            _mockVipRepo = new Mock<IVipPackageRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();

            _mockIdGen.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("VIP_MOCK_ID");
        }
    }
}