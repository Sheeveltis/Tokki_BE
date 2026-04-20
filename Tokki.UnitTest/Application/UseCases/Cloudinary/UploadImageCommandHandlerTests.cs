using FluentAssertions;
using Microsoft.AspNetCore.Http;
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
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static UploadImageCommandHandler CreateHandler(Mock<ICloudinaryService>? cloudinary = null)
        {
            return new UploadImageCommandHandler(
                (cloudinary ?? MockCloudinaryService.GetMock()).Object);
        }

        private static IFormFile CreateFakeFile(string fileName = "test.jpg", long size = 1024)
        {
            var stream = new MemoryStream(new byte[size]);
            var mock = new Mock<IFormFile>();
            mock.Setup(x => x.FileName).Returns(fileName);
            mock.Setup(x => x.Length).Returns(size);
            mock.Setup(x => x.ContentType).Returns("image/jpeg");
            mock.Setup(x => x.OpenReadStream()).Returns(stream);
            return mock.Object;
        }

        // ═══════════════════════════════════════════════════════════
        // Upload_Image_01 | A | Cloudinary returns empty URL → 500 Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CloudinaryReturnsEmptyUrl_ShouldReturn500()
        {
            // Arrange
            var mockCloudinary = MockCloudinaryService.GetMock();
            mockCloudinary.Setup(x => x.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                          .ReturnsAsync(string.Empty);

            var command = new UploadImageCommand { File = CreateFakeFile(), FolderName = "tokki/images" };

            // Act
            var result = await CreateHandler(mockCloudinary).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup     = "Upload Image",
                TestCaseID        = "Upload_Image_01",
                Description       = "Cloudinary service returns an empty URL string",
                ExpectedResult    = "Return 500 Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UploadImageAsync returns empty string", "IsSuccess = false", "StatusCode = 500" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Upload_Image_02 | N | Valid file + folder → 200 Success with URL
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidFile_ShouldReturnImageUrl()
        {
            // Arrange
            var command = new UploadImageCommand { File = CreateFakeFile(), FolderName = "tokki/images" };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("https://cloudinary.com/image/test.jpg");

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup     = "Upload Image",
                TestCaseID        = "Upload_Image_02",
                Description       = "Upload valid JPEG image to specified folder",
                ExpectedResult    = "Return 200 Success with Cloudinary URL",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid IFormFile", "Cloudinary returns valid URL" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Upload_Image_03 | A | Cloudinary throws exception → 500 Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CloudinaryThrowsException_ShouldReturn500()
        {
            // Arrange
            var mockCloudinary = MockCloudinaryService.GetMock();
            mockCloudinary.Setup(x => x.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                          .ThrowsAsync(new Exception("Cloudinary connection failed"));

            var command = new UploadImageCommand { File = CreateFakeFile(), FolderName = "tokki/images" };

            // Act
            var result = await CreateHandler(mockCloudinary).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup     = "Upload Image",
                TestCaseID        = "Upload_Image_03",
                Description       = "Cloudinary service throws an unhandled exception during upload",
                ExpectedResult    = "Exception is caught, return 500 with error message",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UploadImageAsync throws Exception", "try/catch block active" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Upload_Image_04 | N | Returned URL contains expected domain
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidFile_ReturnedUrlContainsCloudinaryDomain()
        {
            // Arrange
            var command = new UploadImageCommand { File = CreateFakeFile(), FolderName = "tokki/images" };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain("cloudinary.com");

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup     = "Upload Image",
                TestCaseID        = "Upload_Image_04",
                Description       = "Verify the returned URL references the Cloudinary CDN domain",
                ExpectedResult    = "URL contains 'cloudinary.com'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "URL format validation", "CDN domain check" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Upload_Image_05 | B | Large file (5 MB) → behaves same as normal
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_LargeFile_ShouldStillReturnSuccess()
        {
            // Arrange
            var command = new UploadImageCommand
            {
                File       = CreateFakeFile("large.jpg", 5 * 1024 * 1024), // 5 MB
                FolderName = "tokki/images"
            };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup     = "Upload Image",
                TestCaseID        = "Upload_Image_05",
                Description       = "Upload an image at the upper boundary size (5 MB)",
                ExpectedResult    = "Handler does not reject large files; returns Success",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "File size boundary at 5 MB", "No client-side size validation in handler" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Upload_Image_06 | N | Custom folder name is forwarded to service
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidFile_ShouldForwardFolderNameToService()
        {
            // Arrange
            var mockCloudinary = MockCloudinaryService.GetMock();
            var command = new UploadImageCommand { File = CreateFakeFile(), FolderName = "tokki/avatars" };

            // Act
            await CreateHandler(mockCloudinary).Handle(command, CancellationToken.None);

            // Assert
            mockCloudinary.Verify(
                x => x.UploadImageAsync(It.IsAny<IFormFile>(), "tokki/avatars"),
                Times.Once);

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup     = "Upload Image",
                TestCaseID        = "Upload_Image_06",
                Description       = "Verify the handler passes the FolderName exactly to the Cloudinary service",
                ExpectedResult    = "UploadImageAsync called with correct folder argument",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Moq.Verify confirms folder forwarding", "FolderName = 'tokki/avatars'" }
            });
        }
    }
}