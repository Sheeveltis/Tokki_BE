using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;

namespace Tokki.UnitTest.Mocks.Services
{
    public static class MockBackgroundJobClient
    {
        public static Mock<IBackgroundJobClient> GetMock()
        {
            var mock = new Mock<IBackgroundJobClient>();
            mock.Setup(x => x.Create(
                    It.IsAny<Job>(),
                    It.IsAny<IState>()))
                .Returns("fake-job-id");
            return mock;
        }

        public static Mock<IBackgroundJobClient> GetThrowingMock()
        {
            var mock = new Mock<IBackgroundJobClient>();
            mock.Setup(x => x.Create(
                    It.IsAny<Job>(),
                    It.IsAny<IState>()))
                .Throws(new Exception("Hangfire connection failed"));
            return mock;
        }
    }
}