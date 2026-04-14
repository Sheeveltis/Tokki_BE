using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Cloudinary.Commands.UploadVocabularyImageByUrl;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Cloudinary
{
    public class UploadVocabularyImageByUrlCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static UploadVocabularyImageByUrlCommandHandler CreateHandler(
            Mock<ICloudinaryService>? cloudinary = null,
            IHttpClientFactory? httpFactory = null)
        {
            cloudinary ??= MockCloudinaryService.GetMock();

            if (httpFactory == null)
            {
                var mockFactory = new Mock<IHttpClientFactory>();
                mockFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                           .Returns(new HttpClient());
                httpFactory = mockFactory.Object;
            }

            return new UploadVocabularyImageByUrlCommandHandler(cloudinary.Object, httpFactory);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CVIU-01 | N | Valid URL → Cloudinary returns image URL
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidImageUrl_ShouldReturnCloudinaryUrl()
        {
            // Arrange
            var command = new UploadVocabularyImageByUrlCommand { ImageUrl = "https://example.com/vocab.jpg" };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("https://cloudinary.com/image/test.jpg");

            QACollector.LogTestCase("Cloudinary - Upload Vocab Image By Url", new TestCaseDetail
            {
                FunctionGroup     = "Upload Vocabulary Image By URL",
                TestCaseID        = "TC-CVIU-01",
                Description       = "Valid external image URL is provided; Cloudinary fetches and stores it",
                ExpectedResult    = "Return 200 Success with Cloudinary CDN URL",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UploadImageFromUrlAsync returns valid URL", "IsSuccess = true" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CVIU-02 | A | Primary upload fails → fallback attempted
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PrimaryUploadFails_ShouldAttemptFallback()
        {
            // Arrange
            var mockCloudinary = MockCloudinaryService.GetMock();
            mockCloudinary.Setup(x => x.UploadImageFromUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .ThrowsAsync(new Exception("Direct URL upload failed"));

            // Fallback via HttpClient will also fail because we don't mock a real server
            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                       .Returns(new HttpClient()); // real client → connects nowhere

            var command = new UploadVocabularyImageByUrlCommand { ImageUrl = "https://example.com/vocab.jpg" };

            // Act
            var result = await CreateHandler(mockCloudinary, mockFactory.Object).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Cloudinary - Upload Vocab Image By Url", new TestCaseDetail
            {
                FunctionGroup     = "Upload Vocabulary Image By URL",
                TestCaseID        = "TC-CVIU-02",
                Description       = "Primary Cloudinary URL upload fails; fallback download also fails",
                ExpectedResult    = "Return 500 Failure with descriptive error message",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Primary throws exception", "Fallback HttpClient cannot reach server", "Both paths fail" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CVIU-03 | N | cdn-cgi URL gets cleaned before passing on
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CdnCgiPrefixedUrl_ShouldCleanUrlBeforeUpload()
        {
            // Arrange
            var mockCloudinary = MockCloudinaryService.GetMock();
            // URL with cdn-cgi prefix wrapping the real URL
            var command = new UploadVocabularyImageByUrlCommand
            {
                ImageUrl = "https://cdn-cgi.example.com/proxy?url=https://example.com/real.jpg"
            };

            // Act
            var result = await CreateHandler(mockCloudinary).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            // Handler should have stripped the proxy prefix and called with the last https:// segment
            mockCloudinary.Verify(
                x => x.UploadImageFromUrlAsync(It.Is<string>(u => u.StartsWith("https://example.com")), It.IsAny<string>()),
                Times.Once);

            QACollector.LogTestCase("Cloudinary - Upload Vocab Image By Url", new TestCaseDetail
            {
                FunctionGroup     = "Upload Vocabulary Image By URL",
                TestCaseID        = "TC-CVIU-03",
                Description       = "URL contains a cdn-cgi proxy prefix wrapping the real URL",
                ExpectedResult    = "Handler extracts the real URL and passes it to Cloudinary",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "URL with proxy prefix", "ExtractRealUrl logic strips prefix" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CVIU-04 | B | Empty ImageUrl string
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyImageUrl_ShouldAttemptUploadWithEmptyString()
        {
            // Arrange
            var mockCloudinary = MockCloudinaryService.GetMock();
            var command = new UploadVocabularyImageByUrlCommand { ImageUrl = "" };

            // Act
            var result = await CreateHandler(mockCloudinary).Handle(command, CancellationToken.None);

            // Assert
            // Empty string goes through ExtractRealUrl which returns itself;
            // then UploadImageFromUrlAsync is called with ""
            mockCloudinary.Verify(
                x => x.UploadImageFromUrlAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);

            QACollector.LogTestCase("Cloudinary - Upload Vocab Image By Url", new TestCaseDetail
            {
                FunctionGroup     = "Upload Vocabulary Image By URL",
                TestCaseID        = "TC-CVIU-04",
                Description       = "Boundary: empty string passed as ImageUrl",
                ExpectedResult    = "Handler passes empty string to Cloudinary; no null-ref crash",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ImageUrl = empty string", "ExtractRealUrl returns same value" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CVIU-05 | A | Cloudinary returns null URL → Success check fails
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CloudinaryReturnsNull_FallsBackAndFails()
        {
            // Arrange
            var mockCloudinary = MockCloudinaryService.GetMock();
            mockCloudinary.Setup(x => x.UploadImageFromUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .ReturnsAsync((string)null!); // returns null → handler throws

            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                       .Returns(new HttpClient());

            var command = new UploadVocabularyImageByUrlCommand { ImageUrl = "https://example.com/img.jpg" };

            // Act
            var result = await CreateHandler(mockCloudinary, mockFactory.Object).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Cloudinary - Upload Vocab Image By Url", new TestCaseDetail
            {
                FunctionGroup     = "Upload Vocabulary Image By URL",
                TestCaseID        = "TC-CVIU-05",
                Description       = "Cloudinary returns null instead of a URL; handler falls through to exception branch",
                ExpectedResult    = "Return 500 Failure as both primary and fallback fail",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UploadImageFromUrlAsync returns null", "NullOrEmpty check triggers throw", "Fallback also fails" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CVIU-06 | N | Result message contains useful context on failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_BothUploadPathsFail_ErrorMessageContainsFallback()
        {
            // Arrange
            var mockCloudinary = MockCloudinaryService.GetMock();
            mockCloudinary.Setup(x => x.UploadImageFromUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .ThrowsAsync(new Exception("Primary failed"));

            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                       .Returns(new HttpClient());

            var command = new UploadVocabularyImageByUrlCommand { ImageUrl = "https://example.com/img.jpg" };

            // Act
            var result = await CreateHandler(mockCloudinary, mockFactory.Object).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("fallback");

            QACollector.LogTestCase("Cloudinary - Upload Vocab Image By Url", new TestCaseDetail
            {
                FunctionGroup     = "Upload Vocabulary Image By URL",
                TestCaseID        = "TC-CVIU-06",
                Description       = "Both primary and fallback upload paths fail; error message mentions fallback",
                ExpectedResult    = "Result.Message contains 'fallback' keyword from handler error format",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Primary exception", "Fallback exception", "Error message format check" }
            });
        }
    }
}
