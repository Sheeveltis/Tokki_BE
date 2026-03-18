using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Cloudinary.Commands.UploadImage;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Cloudinary
{
    public class UploadImageCommandHandlerTests
    {
        private UploadImageCommandHandler CreateHandler(
            Mock<ICloudinaryService>? cloudinary = null)
        {
            return new UploadImageCommandHandler(
                (cloudinary ?? MockCloudinaryService.GetMock()).Object);
        }

        private IFormFile CreateFakeFile(string fileName = "test.jpg", long size = 1024)
        {
            var stream = new MemoryStream(new byte[size]);
            var mock = new Mock<IFormFile>();
            mock.Setup(x => x.FileName).Returns(fileName);
            mock.Setup(x => x.Length).Returns(size);
            mock.Setup(x => x.ContentType).Returns("image/jpeg");
            mock.Setup(x => x.OpenReadStream()).Returns(stream);
            return mock.Object;
        }

        [Fact]
        public async Task Handle_CloudinaryReturnsEmptyUrl_ShouldReturn500()
        {
            var command = new UploadImageCommand
            {
                File = CreateFakeFile(),
                FolderName = "tokki/images"
            };

            var mockCloudinary = MockCloudinaryService.GetMock();
            mockCloudinary.Setup(x => x.UploadImageAsync(
                        It.IsAny<IFormFile>(),
                        It.IsAny<string>()))
                          .ReturnsAsync(string.Empty); // trả về empty → failure

            var handler = CreateHandler(cloudinary: mockCloudinary);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup = "Upload Image",
                TestCaseID = "TC-IMG-UPL-01",
                Description = "Cloudinary trả về URL rỗng → return 500 failure",
                ExpectedResult = "Return 500 Failure",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "UploadImageAsync returns empty string",
                    "Return 500"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidFile_ShouldReturnImageUrl()
        {
            var command = new UploadImageCommand
            {
                File = CreateFakeFile(),
                FolderName = "tokki/images"
            };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("https://cloudinary.com/image/test.jpg");

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup = "Upload Image",
                TestCaseID = "TC-IMG-UPL-02",
                Description = "Upload ảnh hợp lệ → trả về URL từ Cloudinary",
                ExpectedResult = "Return Success, Data = image URL",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid file",
                    "Cloudinary returns valid URL",
                    "Return Success"
                }
            });
        }

        [Fact]
        public async Task Handle_CloudinaryThrowsException_ShouldReturn500()
        {
            var command = new UploadImageCommand
            {
                File = CreateFakeFile(),
                FolderName = "tokki/images"
            };

            var mockCloudinary = MockCloudinaryService.GetMock();
            mockCloudinary.Setup(x => x.UploadImageAsync(
                        It.IsAny<IFormFile>(),
                        It.IsAny<string>()))
                          .ThrowsAsync(new Exception("Cloudinary connection failed"));

            var handler = CreateHandler(cloudinary: mockCloudinary);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup = "Upload Image",
                TestCaseID = "TC-IMG-UPL-03",
                Description = "Cloudinary throw exception → catch và return 500",
                ExpectedResult = "Return 500 với message lỗi",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "UploadImageAsync throws Exception",
                    "Caught in try/catch",
                    "Return 500"
                }
            });
        }
    }
}