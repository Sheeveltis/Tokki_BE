using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.Commands.ImportPronunciationExample;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class ImportPronunciationExampleCommandHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static ImportPronunciationExampleCommandHandler CreateHandler(
            Mock<IExcelService>? excelService = null,
            Mock<IPronunciationExampleRepository>? exampleRepo = null,
            Mock<ICloudinaryService>? cloudinaryService = null,
            Mock<ISpeechService>? ttsService = null,
            Mock<IIdGeneratorService>? idGenerator = null)
        {
            return new ImportPronunciationExampleCommandHandler(
                (excelService ?? new Mock<IExcelService>()).Object,
                (exampleRepo ?? new Mock<IPronunciationExampleRepository>()).Object,
                (cloudinaryService ?? new Mock<ICloudinaryService>()).Object,
                (ttsService ?? new Mock<ISpeechService>()).Object,
                (idGenerator ?? new Mock<IIdGeneratorService>()).Object,
                new Mock<ILogger<ImportPronunciationExampleCommandHandler>>().Object);
        }

        private static IFormFile GetMockFile()
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns("examples.xlsx");
            return mock.Object;
        }

        private static ImportPronunciationExampleCommand GetCommand() =>
            new() { File = GetMockFile(), UserId = "USER-001" };

        private static PronunciationExampleExcelDTO GetSampleDto(string raw = "가나다") =>
            new() { PronunciationRuleId = "RULE-1", TargetScript = "가나다", RawScript = raw, PhoneticScript = "ga na da", Meaning = "Korean alphabet", SortOrder = 1 };

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IPE-01 | A | Excel returns null → EXCEL_EMPTY failure
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExtractReturnsNull_ShouldReturnExcelEmpty()
        {
            // Arrange
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractExampleDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync((List<PronunciationExampleExcelDTO>?)null);

            // Act
            var result = await CreateHandler(excelService: mockExcel).Handle(GetCommand(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy dữ liệu hợp lệ");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Pronunciation", new TestCaseDetail
            {
                FunctionGroup     = "ImportPronunciationExample",
                TestCaseID        = "TC-EXC-IPE-01",
                Description       = "ExtractExampleDataAsync returns null",
                ExpectedResult    = "Return Failure EXCEL_EMPTY",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "extractedData == null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IPE-02 | A | Excel returns empty list → EXCEL_EMPTY failure
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExtractReturnsEmpty_ShouldReturnExcelEmpty()
        {
            // Arrange
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractExampleDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(new List<PronunciationExampleExcelDTO>());

            // Act
            var result = await CreateHandler(excelService: mockExcel).Handle(GetCommand(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy dữ liệu hợp lệ");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Pronunciation", new TestCaseDetail
            {
                FunctionGroup     = "ImportPronunciationExample",
                TestCaseID        = "TC-EXC-IPE-02",
                Description       = "ExtractExampleDataAsync returns empty list",
                ExpectedResult    = "Return Failure EXCEL_EMPTY",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!extractedData.Any()" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IPE-03 | N | Valid data → TTS + Cloudinary called, entity created
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidData_ShouldCallTtsAndCloudinaryAndInsert()
        {
            // Arrange
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractExampleDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(new List<PronunciationExampleExcelDTO> { GetSampleDto() });

            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ReturnsAsync(new byte[] { 1, 2, 3 });

            var mockCloudinary = new Mock<ICloudinaryService>();
            mockCloudinary.Setup(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                          .ReturnsAsync("audio-url");

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.Generate(10)).Returns("EXID-001");

            var mockRepo = new Mock<IPronunciationExampleRepository>();
            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Domain.Entities.PronunciationExample>>()))
                    .Returns(Task.CompletedTask);

            // Act
            var result = await CreateHandler(
                excelService: mockExcel,
                exampleRepo: mockRepo,
                cloudinaryService: mockCloudinary,
                ttsService: mockTts,
                idGenerator: mockIdGen
            ).Handle(GetCommand(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.SuccessList.Should().HaveCount(1);
            mockTts.Verify(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()), Times.Once);
            mockCloudinary.Verify(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            mockRepo.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<Domain.Entities.PronunciationExample>>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Excel - Import Pronunciation", new TestCaseDetail
            {
                FunctionGroup     = "ImportPronunciationExample",
                TestCaseID        = "TC-EXC-IPE-03",
                Description       = "Valid data → TTS synthesizes audio, Cloudinary uploads, entity inserted",
                ExpectedResult    = "Return 200, SuccessList = 1, all services called",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "extractedData.Count > 0", "TTS and Cloudinary succeed", "AddRangeAsync called" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IPE-04 | N | TTS/Cloudinary fails → entity still created (no audio), goes to success
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TtsThrows_ShouldStillCreateEntityWithNoAudio()
        {
            // Arrange
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractExampleDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(new List<PronunciationExampleExcelDTO> { GetSampleDto() });

            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ThrowsAsync(new Exception("TTS offline")); // TTS fails

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.Generate(10)).Returns("EXID-001");

            var mockRepo = new Mock<IPronunciationExampleRepository>();
            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Domain.Entities.PronunciationExample>>()))
                    .Returns(Task.CompletedTask);

            // Act
            var result = await CreateHandler(
                excelService: mockExcel,
                exampleRepo: mockRepo,
                ttsService: mockTts,
                idGenerator: mockIdGen
            ).Handle(GetCommand(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            // Entity is still created but AudioUrl will be null (TTS failure is swallowed by inner try/catch)
            result.Data!.SuccessList.Should().HaveCount(1);

            // Excel Log
            QACollector.LogTestCase("Excel - Import Pronunciation", new TestCaseDetail
            {
                FunctionGroup     = "ImportPronunciationExample",
                TestCaseID        = "TC-EXC-IPE-04",
                Description       = "TTS service throws → inner try/catch swallows it, entity created without audio",
                ExpectedResult    = "Return 200, SuccessList = 1 with null AudioUrl",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "inner catch for TTS/Cloudinary", "entity still added to newEntities" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IPE-05 | A | AddRangeAsync throws → DatabaseError failure
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AddRangeThrows_ShouldReturnDatabaseError()
        {
            // Arrange
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractExampleDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(new List<PronunciationExampleExcelDTO> { GetSampleDto() });

            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ReturnsAsync(new byte[] { 1, 2, 3 });

            var mockCloudinary = new Mock<ICloudinaryService>();
            mockCloudinary.Setup(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                          .ReturnsAsync("audio-url");

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.Generate(10)).Returns("EXID-001");

            var mockRepo = new Mock<IPronunciationExampleRepository>();
            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Domain.Entities.PronunciationExample>>()))
                    .ThrowsAsync(new Exception("DB write failed"));

            // Act
            var result = await CreateHandler(
                excelService: mockExcel,
                exampleRepo: mockRepo,
                cloudinaryService: mockCloudinary,
                ttsService: mockTts,
                idGenerator: mockIdGen
            ).Handle(GetCommand(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            // Excel Log
            QACollector.LogTestCase("Excel - Import Pronunciation", new TestCaseDetail
            {
                FunctionGroup     = "ImportPronunciationExample",
                TestCaseID        = "TC-EXC-IPE-05",
                Description       = "AddRangeAsync throws exception during database save",
                ExpectedResult    = "Return Failure DatabaseError",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception ex) inside AddRangeAsync => DatabaseError" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IPE-06 | N | Multiple items → summary message has correct counts
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MultipleItems_ShouldReturnCorrectSummaryMessage()
        {
            // Arrange
            var items = new List<PronunciationExampleExcelDTO>
            {
                GetSampleDto("가나"),
                GetSampleDto("다라마")
            };

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractExampleDataAsync(It.IsAny<IFormFile>())).ReturnsAsync(items);

            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ReturnsAsync(new byte[] { 0x01 });

            var mockCloudinary = new Mock<ICloudinaryService>();
            mockCloudinary.Setup(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                          .ReturnsAsync("audio-url");

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.SetupSequence(x => x.Generate(10)).Returns("ID1").Returns("ID2");

            var mockRepo = new Mock<IPronunciationExampleRepository>();
            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Domain.Entities.PronunciationExample>>()))
                    .Returns(Task.CompletedTask);

            // Act
            var result = await CreateHandler(
                excelService: mockExcel,
                exampleRepo: mockRepo,
                cloudinaryService: mockCloudinary,
                ttsService: mockTts,
                idGenerator: mockIdGen
            ).Handle(GetCommand(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.SuccessList.Should().HaveCount(2);
            result.Message.Should().Contain("Thành công: 2");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Pronunciation", new TestCaseDetail
            {
                FunctionGroup     = "ImportPronunciationExample",
                TestCaseID        = "TC-EXC-IPE-06",
                Description       = "2 valid items processed → summary message shows correct counts",
                ExpectedResult    = "Return 200, SuccessList = 2, summary 'Thành công: 2'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "extractedData.Count == 2", "all succeed" }
            });
        }
    }
}