using Moq;
using Tokki.Application.IServices;

namespace Tokki.UnitTest.Mocks.Services
{
    public static class MockIdGeneratorService
    {
        public static Mock<IIdGeneratorService> GetMock()
        {
            var mock = new Mock<IIdGeneratorService>();
            mock.Setup(x => x.Generate(It.IsAny<int>())).Returns("TK_FAKE_ID_999");
            return mock;
        }
    }
}