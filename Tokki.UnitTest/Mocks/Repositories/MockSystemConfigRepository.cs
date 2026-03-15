using Moq;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockSystemConfigRepository
    {
        public static Mock<ISystemConfigRepository> GetMock(string otpSeconds = "300")
        {
            var mock = new Mock<ISystemConfigRepository>();

            mock.Setup(x => x.GetValueByKeyAsync(It.IsAny<string>()))
                .ReturnsAsync(otpSeconds);

            return mock;
        }
    }
}
