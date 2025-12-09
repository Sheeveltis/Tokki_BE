using Microsoft.Extensions.Logging; // Nếu cần Logger sau này
using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;

namespace Tokki.UnitTests.Common.Bases
{
    public class AccountTestBase
    {
        protected readonly Mock<IAccountRepository> _mockAccountRepo;
        protected readonly Mock<ISystemConfigRepository> _mockSystemConfigRepo;
        protected readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
        protected readonly Mock<IIdGeneratorService> _mockIdGenerator;

        public AccountTestBase()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockSystemConfigRepo = new Mock<ISystemConfigRepository>();
            _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
            _mockIdGenerator = new Mock<IIdGeneratorService>();
        }
    }
}