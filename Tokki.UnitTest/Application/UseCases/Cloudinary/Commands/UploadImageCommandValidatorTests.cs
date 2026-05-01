using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.Cloudinary.Commands.UploadImage;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Cloudinary.Commands
{
    public class UploadImageCommandValidatorTests
    {
        private readonly UploadImageCommandValidator _validator;

        public UploadImageCommandValidatorTests()
        {
            _validator = new UploadImageCommandValidator();
        }

        private Mock<IFormFile> CreateMockFile(string contentType, long length)
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.ContentType).Returns(contentType);
            mock.Setup(f => f.Length).Returns(length);
            return mock;
        }

        // UploadImageCommandValidator_01 | A | File is Null -> Error
        [Fact]
        public void Validate_NullFile_ShouldHaveError()
        {
            var command = new UploadImageCommand { File = null! };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.File)
                  .WithErrorMessage("Vui lòng chọn file ảnh.");

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup = "UploadImageCommandValidator",
                TestCaseID = "UploadImageCommandValidator_01",
                Description = "Null File triggers NotNull rule",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "File = null" }
            });
        }

        // UploadImageCommandValidator_02 | A | File Length is 0 -> Error
        [Fact]
        public void Validate_EmptyFile_ShouldHaveError()
        {
            var mockFile = CreateMockFile("image/jpeg", 0);
            var command = new UploadImageCommand { File = mockFile.Object };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.File)
                  .WithErrorMessage("File ảnh không được rỗng.");

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup = "UploadImageCommandValidator",
                TestCaseID = "UploadImageCommandValidator_02",
                Description = "0 byte file returns error",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Length = 0" }
            });
        }

        // UploadImageCommandValidator_03 | A | File Size Exceeds 5MB -> Error
        [Fact]
        public void Validate_SizeExceeded_ShouldHaveError()
        {
            var mockFile = CreateMockFile("image/jpeg", 5 * 1024 * 1024 + 1); // 5MB + 1 byte
            var command = new UploadImageCommand { File = mockFile.Object };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.File)
                  .WithErrorMessage("Kích thước ảnh không được vượt quá 5MB.");

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup = "UploadImageCommandValidator",
                TestCaseID = "UploadImageCommandValidator_03",
                Description = "Size boundary checks strictly limit 5MB",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Length > 5MB" }
            });
        }

        // UploadImageCommandValidator_04 | A | Invalid ContentType -> Error
        [Fact]
        public void Validate_InvalidContentType_ShouldHaveError()
        {
            var mockFile = CreateMockFile("application/pdf", 1024);
            var command = new UploadImageCommand { File = mockFile.Object };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.File)
                  .WithErrorMessage("Định dạng không hợp lệ. Chỉ chấp nhận: .jpg, .jpeg, .png, .webp");

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup = "UploadImageCommandValidator",
                TestCaseID = "UploadImageCommandValidator_04",
                Description = "PDF type returns error per MIME matching array",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ContentType = application/pdf" }
            });
        }

        // UploadImageCommandValidator_05 | N | Valid Size and Valid ContentType (jpg) -> Success
        [Fact]
        public void Validate_ValidJpeg_ShouldNotHaveError()
        {
            var mockFile = CreateMockFile("image/jpeg", 1024);
            var command = new UploadImageCommand { File = mockFile.Object };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup = "UploadImageCommandValidator",
                TestCaseID = "UploadImageCommandValidator_05",
                Description = "Perfect JPEG payload passes all rules",
                ExpectedResult = "No errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Size 1kb + MIME Valid" }
            });
        }

        // UploadImageCommandValidator_06 | N | Valid Size Limit exact and Valid ContentType (webp) -> Success
        [Fact]
        public void Validate_ValidWebpAtSizeLimit_ShouldNotHaveError()
        {
            var mockFile = CreateMockFile("image/webp", 5 * 1024 * 1024); // Exactly 5MB
            var command = new UploadImageCommand { File = mockFile.Object };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup = "UploadImageCommandValidator",
                TestCaseID = "UploadImageCommandValidator_06",
                Description = "Exact maximum size allowed without tripping bounds error",
                ExpectedResult = "No errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Size = 5MB, MIME = webp" }
            });
        }
        // UploadImageCommandValidator_07 | A | File ContentType Null -> Error
        [Fact]
        public void Validate_NullContentType_ShouldHaveError()
        {
            var mockFile = CreateMockFile(null!, 1024);
            var command = new UploadImageCommand { File = mockFile.Object };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.File)
                  .WithErrorMessage("Định dạng không hợp lệ. Chỉ chấp nhận: .jpg, .jpeg, .png, .webp");

            QACollector.LogTestCase("Cloudinary - Upload Image", new TestCaseDetail
            {
                FunctionGroup = "UploadImageCommandValidator",
                TestCaseID = "UploadImageCommandValidator_07",
                Description = "Null ContentType handles securely without NullReferenceException",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ContentType is null" }
            });
        }
    }
}
