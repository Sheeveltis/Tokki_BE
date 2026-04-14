using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.Cloudinary.Commands.UploadAudio;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Cloudinary.Commands
{
    public class UploadAudioCommandValidatorTests
    {
        private readonly UploadAudioCommandValidator _validator;

        public UploadAudioCommandValidatorTests()
        {
            _validator = new UploadAudioCommandValidator();
        }

        private Mock<IFormFile> CreateMockFile(string fileName, long length)
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns(fileName);
            mock.Setup(f => f.Length).Returns(length);
            return mock;
        }

        // TC-CLD-UAV-01 | A | FolderName is Empty -> Error
        [Fact]
        public void Validate_EmptyFolderName_ShouldHaveError()
        {
            var command = new UploadAudioCommand { FolderName = "", AudioFile = CreateMockFile("a.mp3", 100).Object };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.FolderName)
                  .WithErrorMessage("Tên thư mục không được để trống.");

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup = "UploadAudioCommandValidator",
                TestCaseID = "TC-CLD-UAV-01",
                Description = "Empty folder name triggers NotEmpty rule",
                ExpectedResult = "Validation Error on FolderName",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "FolderName = empty" }
            });
        }

        // TC-CLD-UAV-02 | A | AudioFile is Null -> Error
        [Fact]
        public void Validate_NullAudioFile_ShouldHaveError()
        {
            var command = new UploadAudioCommand { FolderName = "folder", AudioFile = null! };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.AudioFile)
                  .WithErrorMessage("File âm thanh là bắt buộc.");

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup = "UploadAudioCommandValidator",
                TestCaseID = "TC-CLD-UAV-02",
                Description = "Null AudioFile triggers NotNull rule",
                ExpectedResult = "Validation Error on AudioFile",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AudioFile = null" }
            });
        }

        // TC-CLD-UAV-03 | A | AudioFile Length is 0 -> Error
        [Fact]
        public void Validate_EmptyAudioFile_ShouldHaveError()
        {
            var mockFile = CreateMockFile("a.mp3", 0);
            var command = new UploadAudioCommand { FolderName = "folder", AudioFile = mockFile.Object };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.AudioFile)
                  .WithErrorMessage("File không được rỗng.");

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup = "UploadAudioCommandValidator",
                TestCaseID = "TC-CLD-UAV-03",
                Description = "0 byte file returns specific exception",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Length = 0" }
            });
        }

        // TC-CLD-UAV-04 | A | AudioFile Invalid Extension -> Error
        [Fact]
        public void Validate_InvalidExtension_ShouldHaveError()
        {
            var mockFile = CreateMockFile("a.txt", 100);
            var command = new UploadAudioCommand { FolderName = "folder", AudioFile = mockFile.Object };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.AudioFile)
                  .WithErrorMessage("Định dạng file không hợp lệ. Chỉ chấp nhận: .mp3, .wav, .ogg, .m4a");

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup = "UploadAudioCommandValidator",
                TestCaseID = "TC-CLD-UAV-04",
                Description = "Extension whitelist violation triggers error",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Extension = .txt" }
            });
        }

        // TC-CLD-UAV-05 | A | AudioFile Exceeds Size -> Error
        [Fact]
        public void Validate_SizeExceeded_ShouldHaveError()
        {
            var mockFile = CreateMockFile("a.mp3", 10485761); // 10MB + 1 byte
            var command = new UploadAudioCommand { FolderName = "folder", AudioFile = mockFile.Object };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.AudioFile)
                  .WithErrorMessage("Dung lượng file không được vượt quá 10MB.");

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup = "UploadAudioCommandValidator",
                TestCaseID = "TC-CLD-UAV-05",
                Description = "Size restriction violation triggers error limit 10MB",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Length > 10MB" }
            });
        }

        // TC-CLD-UAV-06 | N | Valid Audio File -> Success
        [Fact]
        public void Validate_ValidAudio_ShouldNotHaveError()
        {
            var mockFile = CreateMockFile("audio.wav", 1024 * 512); // 512KB
            var command = new UploadAudioCommand { FolderName = "folder", AudioFile = mockFile.Object };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Cloudinary - Upload Audio", new TestCaseDetail
            {
                FunctionGroup = "UploadAudioCommandValidator",
                TestCaseID = "TC-CLD-UAV-06",
                Description = "Perfect payload validates cleanly with valid wav",
                ExpectedResult = "No Errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid Extension, Size < 10MB, Folder provided" }
            });
        }
    }
}
