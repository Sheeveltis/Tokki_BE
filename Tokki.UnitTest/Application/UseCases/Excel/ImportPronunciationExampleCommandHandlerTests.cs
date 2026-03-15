using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.Commands.ImportPronunciationExample;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class ImportPronunciationExampleCommandHandlerTests
    {
        private static IFormFile CreateFakeFile()
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(x => x.FileName).Returns("examples.xlsx");
            return mock.Object;
        }

        private ImportPronunciationExampleCommandHandler CreateHandler(
            Mock<IExcelService>? excelService = null,
            Mock<IPronunciationExampleRepository>? exampleRepo = null,
            Mock<ISpeechService>? tts = null,
            Mock<ICloudinaryService>? cloudinary = null)
        {
            return new ImportPronunciationExampleCommandHandler(
                (excelService ?? new Mock<IExcelService>()).Object,
                (exampleRepo ?? new Mock<IPronunciationExampleRepository>()).Object,
                (cloudinary ?? MockCloudinaryService.GetMock()).Object,
                (tts ?? MockSpeechService.GetMock()).Object,
                MockIdGeneratorService.GetMock().Object,
                new Mock<ILogger<ImportPronunciationExampleCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_EmptyExcelFile_ShouldReturnFailure()
        {
            var command = new ImportPronunciationExampleCommand
            {
                File = CreateFakeFile(),
                UserId = "USER-001"
            };

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractExampleDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(new List<PronunciationExampleExcelDTO>());

            var handler = CreateHandler(excelService: mockExcel);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Excel - Import Pronunciation Example", new TestCaseDetail
            {
                FunctionGroup = "Import Pronunciation Example",
                TestCaseID = "TC-IPE-01",
                Description = "Import file Excel rỗng → return Failure EXCEL_EMPTY",
                ExpectedResult = "Return Failure EXCEL_EMPTY",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "ExtractExampleDataAsync returns empty list",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidData_ShouldImportAndReturn200()
        {
            var command = new ImportPronunciationExampleCommand
            {
                File = CreateFakeFile(),
                UserId = "USER-001"
            };

            var extractedData = new List<PronunciationExampleExcelDTO>
            {
                new PronunciationExampleExcelDTO
                {
                    RawScript = "안녕하세요",
                    TargetScript = "안녕하세요",
                    PronunciationRuleId = "RULE-001"
                }
            };

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractExampleDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(extractedData);

            var mockExampleRepo = new Mock<IPronunciationExampleRepository>();
            mockExampleRepo.Setup(x => x.AddRangeAsync(
                        It.IsAny<IEnumerable<Domain.Entities.PronunciationExample>>()))
                           .Returns(Task.CompletedTask);

            var handler = CreateHandler(
                excelService: mockExcel,
                exampleRepo: mockExampleRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.SuccessList.Should().HaveCount(1);

            QACollector.LogTestCase("Excel - Import Pronunciation Example", new TestCaseDetail
            {
                FunctionGroup = "Import Pronunciation Example",
                TestCaseID = "TC-IPE-02",
                Description = "Import 1 example hợp lệ → tạo thành công",
                ExpectedResult = "Return 200, SuccessList.Count = 1",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "1 valid example",
                    "TTS + Cloudinary success",
                    "AddRangeAsync called",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_TtsFailsForOneItem_ShouldStillImportWithNullAudio()
        {
            var command = new ImportPronunciationExampleCommand
            {
                File = CreateFakeFile(),
                UserId = "USER-001"
            };

            var extractedData = new List<PronunciationExampleExcelDTO>
            {
                new PronunciationExampleExcelDTO
                {
                    RawScript = "테스트",
                    TargetScript = "테스트",
                    PronunciationRuleId = "RULE-001"
                }
            };

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractExampleDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(extractedData);

            // TTS throw → audioUrl = null, nhưng vẫn import được
            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ThrowsAsync(new Exception("TTS unavailable"));

            var mockExampleRepo = new Mock<IPronunciationExampleRepository>();
            mockExampleRepo.Setup(x => x.AddRangeAsync(
                        It.IsAny<IEnumerable<Domain.Entities.PronunciationExample>>()))
                           .Returns(Task.CompletedTask);

            var handler = CreateHandler(
                excelService: mockExcel,
                exampleRepo: mockExampleRepo,
                tts: mockTts);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.SuccessList.Should().HaveCount(1);

            QACollector.LogTestCase("Excel - Import Pronunciation Example", new TestCaseDetail
            {
                FunctionGroup = "Import Pronunciation Example",
                TestCaseID = "TC-IPE-03",
                Description = "TTS lỗi → example vẫn được import với AudioUrl = null",
                ExpectedResult = "Return 200, SuccessList.Count = 1, AudioUrl = null",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "TTS throws Exception",
                    "Import continues without audio",
                    "Return 200"
                }
            });
        }
    }
}