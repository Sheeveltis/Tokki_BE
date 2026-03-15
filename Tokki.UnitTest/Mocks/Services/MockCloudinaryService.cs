using Moq;
using System.Threading.Tasks;
using Tokki.Application.IServices;

namespace Tokki.UnitTest.Mocks.Services
{
    public static class MockCloudinaryService
    {
        public static Mock<ICloudinaryService> GetMock(
            string imageUrl = "https://cloudinary.com/image/test.jpg",
            string audioUrl = "https://cloudinary.com/audio/test.mp3")
        {
            var mock = new Mock<ICloudinaryService>();

            mock.Setup(x => x.UploadImageAsync(
                        It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(),
                        It.IsAny<string>()))
                .ReturnsAsync(imageUrl);

            mock.Setup(x => x.UploadAudioAsync(
                        It.IsAny<byte[]>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                .ReturnsAsync(audioUrl);

            mock.Setup(x => x.UploadImageFromUrlAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                .ReturnsAsync(imageUrl);

            return mock;
        }
    }
}