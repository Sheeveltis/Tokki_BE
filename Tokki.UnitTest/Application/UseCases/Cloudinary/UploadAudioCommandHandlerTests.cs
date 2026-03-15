using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Cloudinary.Commands.UploadAudio;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Cloudinary
{
    public class UploadAudioCommandHandlerTests
    {
        private UploadAudioCommandHandler CreateHandler(
            Mock<ICloudinaryService>? cloudinary = null)
        {
            return new UploadAudioCommandHandler(
                (cloudinary ?? MockCloudinaryService.GetMock()).Object);
        }

        private IFormFile CreateFakeAudioFile(
            string fileName = "test.mp3",
            long size = 1024)
        {
            var bytes = new byte[size];
            var stream = new MemoryStream(bytes);
            var mock = new Mock<IFormFile>();
            mock.Setup(x => x.FileName).Returns(fileName);
            mock.Setup(x => x.Length).Returns(size);
            mock.Setup(x => x.ContentType).Returns("audio/mpeg");
            mock.Setup(x => x.CopyToAsync(
                        It.IsAny<Stream>(),
                        It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((s, ct) =>
                    stream.CopyToAsync(s, ct))
                .Returns(Task.CompletedTask);
            return mock.Object;
        }

        [Fact]
        public async Task Handle_ValidAudioFile_ShouldReturnAudioUrl()
        {
            var command = new UploadAudioCommand
            {
                AudioFile = CreateFakeAudioFile(),
                FolderName = "tokki/audio"
            };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("https://cloudinary.com/audio/test.mp3");

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup = "Upload Audio",
                TestCaseID = "TC-AUD-UPL-01",
                Description = "Upload audio hợp lệ → trả về URL từ Cloudinary",
                ExpectedResult = "Return Success, Data = audio URL",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid audio file",
                    "Cloudinary returns valid URL",
                    "Return Success"
                }
            });
        }

        [Fact]
        public async Task Handle_CloudinaryThrowsException_ShouldReturnFailure()
        {
            var command = new UploadAudioCommand
            {
                AudioFile = CreateFakeAudioFile(),
                FolderName = "tokki/audio"
            };

            var mockCloudinary = MockCloudinaryService.GetMock();
            mockCloudinary.Setup(x => x.UploadAudioAsync(
                        It.IsAny<byte[]>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                          .ThrowsAsync(new Exception("Cloudinary timeout"));

            var handler = CreateHandler(cloudinary: mockCloudinary);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup = "Upload Audio",
                TestCaseID = "TC-AUD-UPL-02",
                Description = "Cloudinary throw exception khi upload audio → return Failure",
                ExpectedResult = "Return Failure với message lỗi",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "UploadAudioAsync throws Exception",
                    "Return Failure"
                }
            });
        }
    }
}