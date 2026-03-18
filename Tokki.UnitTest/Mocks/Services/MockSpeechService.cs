using Moq;
using System.Threading.Tasks;
using Tokki.Application.IServices;

namespace Tokki.UnitTest.Mocks.Services
{
    public static class MockSpeechService
    {
        public static Mock<ISpeechService> GetMock()
        {
            var mock = new Mock<ISpeechService>();

            mock.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                .ReturnsAsync(new byte[] { 0x01, 0x02 });

            return mock;
        }
    }
}