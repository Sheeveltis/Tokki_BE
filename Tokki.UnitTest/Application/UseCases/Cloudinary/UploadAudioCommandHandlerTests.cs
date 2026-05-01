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
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static UploadAudioCommandHandler CreateHandler(Mock<ICloudinaryService>? cloudinary = null)
        {
            return new UploadAudioCommandHandler(
                (cloudinary ?? MockCloudinaryService.GetMock()).Object);
        }

        private static IFormFile CreateFakeAudioFile(string fileName = "test.mp3", long size = 1024)
        {
            var bytes = new byte[size];
            var stream = new MemoryStream(bytes);
            var mock = new Mock<IFormFile>();
            mock.Setup(x => x.FileName).Returns(fileName);
            mock.Setup(x => x.Length).Returns(size);
            mock.Setup(x => x.ContentType).Returns("audio/mpeg");
            mock.Setup(x => x.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((s, ct) => stream.CopyToAsync(s, ct))
                .Returns(Task.CompletedTask);
            return mock.Object;
        }

        // ═══════════════════════════════════════════════════════════
        // Upload_Audio_01 | N | Valid audio file → 200 Success with URL
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidAudioFile_ShouldReturnAudioUrl()
        {
            // Arrange
            var command = new UploadAudioCommand { AudioFile = CreateFakeAudioFile(), FolderName = "tokki/audio" };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("https://cloudinary.com/audio/test.mp3");

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup     = "Upload Audio",
                TestCaseID        = "Upload_Audio_01",
                Description       = "Upload valid MP3 audio file to specified folder",
                ExpectedResult    = "Return 200 Success with Cloudinary audio URL",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid IFormFile (MP3)", "Cloudinary returns valid URL" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Upload_Audio_02 | A | Cloudinary throws exception → Failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CloudinaryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var mockCloudinary = MockCloudinaryService.GetMock();
            mockCloudinary.Setup(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                          .ThrowsAsync(new Exception("Cloudinary timeout"));

            var command = new UploadAudioCommand { AudioFile = CreateFakeAudioFile(), FolderName = "tokki/audio" };

            // Act
            var result = await CreateHandler(mockCloudinary).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup     = "Upload Audio",
                TestCaseID        = "Upload_Audio_02",
                Description       = "Cloudinary service throws a timeout exception during upload",
                ExpectedResult    = "Exception is caught, return Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UploadAudioAsync throws Exception", "try/catch swallows and returns Failure" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Upload_Audio_03 | N | Returned URL contains expected domain
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidAudio_ReturnedUrlContainsCloudinaryDomain()
        {
            // Arrange
            var command = new UploadAudioCommand { AudioFile = CreateFakeAudioFile(), FolderName = "tokki/audio" };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain("cloudinary.com");

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup     = "Upload Audio",
                TestCaseID        = "Upload_Audio_03",
                Description       = "Verify the returned URL references the Cloudinary CDN",
                ExpectedResult    = "URL contains 'cloudinary.com'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "URL domain validation" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Upload_Audio_04 | N | UploadAudioAsync is called exactly once
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidAudio_ShouldCallUploadAudioAsyncOnce()
        {
            // Arrange
            var mockCloudinary = MockCloudinaryService.GetMock();
            var command = new UploadAudioCommand { AudioFile = CreateFakeAudioFile(), FolderName = "tokki/audio" };

            // Act
            await CreateHandler(mockCloudinary).Handle(command, CancellationToken.None);

            // Assert
            mockCloudinary.Verify(
                x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), "tokki/audio"),
                Times.Once);

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup     = "Upload Audio",
                TestCaseID        = "Upload_Audio_04",
                Description       = "Verify handler calls the audio upload service exactly once with correct folder",
                ExpectedResult    = "UploadAudioAsync called once with folder = 'tokki/audio'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Moq.Verify confirms single invocation", "Folder argument forwarded correctly" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Upload_Audio_05 | B | 0-byte audio file → reads empty bytes
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ZeroByteFile_ShouldStillCallService()
        {
            // Arrange
            var mockCloudinary = MockCloudinaryService.GetMock();
            var command = new UploadAudioCommand { AudioFile = CreateFakeAudioFile(size: 0), FolderName = "tokki/audio" };

            // Act
            var result = await CreateHandler(mockCloudinary).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue(); // handler doesn't validate length, service mock succeeds
            mockCloudinary.Verify(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup     = "Upload Audio",
                TestCaseID        = "Upload_Audio_05",
                Description       = "Upload a 0-byte audio file (boundary case for empty stream)",
                ExpectedResult    = "Handler passes empty bytes to service; mock returns Success",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "File size = 0 bytes", "No file-size guard in handler" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Upload_Audio_06 | N | Error message is included in failure result
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CloudinaryThrows_ErrorMessageForwardedInResult()
        {
            // Arrange
            var mockCloudinary = MockCloudinaryService.GetMock();
            mockCloudinary.Setup(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                          .ThrowsAsync(new Exception("Storage quota exceeded"));

            var command = new UploadAudioCommand { AudioFile = CreateFakeAudioFile(), FolderName = "tokki/audio" };

            // Act
            var result = await CreateHandler(mockCloudinary).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Storage quota exceeded");

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup     = "Upload Audio",
                TestCaseID        = "Upload_Audio_06",
                Description       = "Exception message from Cloudinary is surfaced in the result message",
                ExpectedResult    = "Result.Message contains the original exception text",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Exception message forwarding", "Failure message contains exception detail" }
            });
        }
    }
}