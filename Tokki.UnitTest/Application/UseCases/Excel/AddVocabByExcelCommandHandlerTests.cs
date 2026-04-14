using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.Commands.AddVocabByExcel;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class AddVocabByExcelCommandHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static AddVocabByExcelCommandHandler CreateHandler(
            Mock<IExcelService>? excelService = null,
            Mock<IVocabularyRepository>? vocabRepo = null,
            Mock<ICloudinaryService>? cloudinaryService = null,
            Mock<ISpeechService>? ttsService = null,
            Mock<IIdGeneratorService>? idGenerator = null,
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null)
        {
            return new AddVocabByExcelCommandHandler(
                (excelService ?? new Mock<IExcelService>()).Object,
                (vocabRepo ?? new Mock<IVocabularyRepository>()).Object,
                (cloudinaryService ?? new Mock<ICloudinaryService>()).Object,
                new Mock<ILogger<AddVocabByExcelCommandHandler>>().Object,
                (ttsService ?? new Mock<ISpeechService>()).Object,
                (idGenerator ?? new Mock<IIdGeneratorService>()).Object,
                (vocabTopicRepo ?? new Mock<IVocabularyTopicRepository>()).Object);
        }

        private static IFormFile GetMockFile()
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns("test_vocab.xlsx");
            return mock.Object;
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-ADD-01 | A | Excel returns null → ExcelNoValidDataFound
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExcelReturnsNull_ShouldReturnFailure()
        {
            // Arrange
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractVocabularyDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync((List<VocabularyExcelDTO>?)null);
            var command = new AddVocabByExcelCommand { File = GetMockFile(), StaffId = "S1", TopicId = null };

            // Act
            var result = await CreateHandler(excelService: mockExcel).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be(AppErrors.ExcelNoValidDataFound.Description);

            // Excel Log
            QACollector.LogTestCase("Excel - Add Vocab", new TestCaseDetail
            {
                FunctionGroup     = "AddVocabByExcel",
                TestCaseID        = "TC-EXC-ADD-01",
                Description       = "Excel parsing service returns null/empty list",
                ExpectedResult    = "Return Failure ExcelNoValidDataFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "extractedVocabs == null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-ADD-02 | A | Excel returns empty list → ExcelNoValidDataFound
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExcelReturnsEmpty_ShouldReturnFailure()
        {
            // Arrange
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractVocabularyDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(new List<VocabularyExcelDTO>());
            var command = new AddVocabByExcelCommand { File = GetMockFile(), StaffId = "S1" };

            // Act
            var result = await CreateHandler(excelService: mockExcel).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be(AppErrors.ExcelNoValidDataFound.Description);

            // Excel Log
            QACollector.LogTestCase("Excel - Add Vocab", new TestCaseDetail
            {
                FunctionGroup     = "AddVocabByExcel",
                TestCaseID        = "TC-EXC-ADD-02",
                Description       = "Excel parsing service returns empty list",
                ExpectedResult    = "Return Failure ExcelNoValidDataFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!extractedVocabs.Any()" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-ADD-03 | N | Duplicate items in file → FailureList with 1 item, return 200
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicateInFile_ShouldReturn200WithFailureList()
        {
            // Arrange
            var dtos = new List<VocabularyExcelDTO>
            {
                new() { Text = "Apple", Definition = "Táo" },
                new() { Text = "Apple", Definition = "Táo" } // duplicate
            };
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractVocabularyDataAsync(It.IsAny<IFormFile>())).ReturnsAsync(dtos);

            var mockVocabRepo = new Mock<IVocabularyRepository>();
            mockVocabRepo.Setup(x => x.GetExistingVocabEntitiesAsync(It.IsAny<List<(string Text, string Definition)>>()))
                         .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary>());

            var command = new AddVocabByExcelCommand { File = GetMockFile(), StaffId = "S1", TopicId = null };

            // Act
            var result = await CreateHandler(excelService: mockExcel, vocabRepo: mockVocabRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data!.FailureList.Should().HaveCount(1);
            result.Data.FailureList[0].Reason.Should().Contain("lặp lại nhiều lần");

            // Excel Log
            QACollector.LogTestCase("Excel - Add Vocab", new TestCaseDetail
            {
                FunctionGroup     = "AddVocabByExcel",
                TestCaseID        = "TC-EXC-ADD-03",
                Description       = "Duplicate non-DB items in file populate FailureList, stops processing",
                ExpectedResult    = "Return 200 with error string, FailureList = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "isDuplicateInFile == true" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-ADD-04 | N | Existing vocab without TopicId → goes to FailureList
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExistingVocabNoTopic_ShouldAddToFailureList()
        {
            // Arrange
            var dtos = new List<VocabularyExcelDTO> { new() { Text = "Apple", Definition = "Táo" } };
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractVocabularyDataAsync(It.IsAny<IFormFile>())).ReturnsAsync(dtos);

            var existingDb = new List<Tokki.Domain.Entities.Vocabulary> { new() { VocabularyId = "V1", Text = "Apple", Definition = "Táo" } };
            var mockVocabRepo = new Mock<IVocabularyRepository>();
            mockVocabRepo.Setup(x => x.GetExistingVocabEntitiesAsync(It.IsAny<List<(string Text, string Definition)>>()))
                         .ReturnsAsync(existingDb);

            var command = new AddVocabByExcelCommand { File = GetMockFile(), TopicId = null, StaffId = "S1" };

            // Act
            var result = await CreateHandler(excelService: mockExcel, vocabRepo: mockVocabRepo).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.FailureList.Should().HaveCount(1);
            result.Data.FailureList[0].Reason.Should().Contain("đã tồn tại trong hệ thống");

            // Excel Log
            QACollector.LogTestCase("Excel - Add Vocab", new TestCaseDetail
            {
                FunctionGroup     = "AddVocabByExcel",
                TestCaseID        = "TC-EXC-ADD-04",
                Description       = "Existing vocab without TopicId goes to FailureList",
                ExpectedResult    = "FailureList = 1, return 200 with error message",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "existingMatch != null && TopicId == null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-ADD-05 | N | New vocab with image & TTS → inserts to DB
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NewVocabWithImageAndTts_ShouldCreateAndCallDependencies()
        {
            // Arrange
            var dtos = new List<VocabularyExcelDTO>
            {
                new() { Text = "Banana", Definition = "Chuối", ImageUrl = "http://img.com/b.png" }
            };
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractVocabularyDataAsync(It.IsAny<IFormFile>())).ReturnsAsync(dtos);

            var mockVocabRepo = new Mock<IVocabularyRepository>();
            mockVocabRepo.Setup(x => x.GetExistingVocabEntitiesAsync(It.IsAny<List<(string Text, string Definition)>>()))
                         .ReturnsAsync(new List<Tokki.Domain.Entities.Vocabulary>());

            var mockCloudinary = new Mock<ICloudinaryService>();
            mockCloudinary.Setup(x => x.UploadImageFromUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .ReturnsAsync("cloud-img-url");
            mockCloudinary.Setup(x => x.UploadAudioAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                          .ReturnsAsync("cloud-audio-url");

            var mockTts = new Mock<ISpeechService>();
            mockTts.Setup(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()))
                   .ReturnsAsync(new byte[] { 1, 2, 3 });

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.Generate(It.IsAny<int>())).Returns("ID123");

            var command = new AddVocabByExcelCommand { File = GetMockFile(), StaffId = "S1", TopicId = null };

            // Act
            var result = await CreateHandler(
                excelService: mockExcel,
                vocabRepo: mockVocabRepo,
                cloudinaryService: mockCloudinary,
                ttsService: mockTts,
                idGenerator: mockIdGen
            ).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.AddedNewVocabList.Should().HaveCount(1);
            mockCloudinary.Verify(x => x.UploadImageFromUrlAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            mockTts.Verify(x => x.SynthesizeKoreanAudioAsync(It.IsAny<string>()), Times.Once);
            mockVocabRepo.Verify(x => x.AddRangeAsync(It.IsAny<List<Tokki.Domain.Entities.Vocabulary>>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Excel - Add Vocab", new TestCaseDetail
            {
                FunctionGroup     = "AddVocabByExcel",
                TestCaseID        = "TC-EXC-ADD-05",
                Description       = "Valid new vocab synthesized, uploaded to Cloudinary, and inserted to DB",
                ExpectedResult    = "Return 200, AddedNewVocabList = 1, all external services called",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "newItemsToProcess.Count > 0", "ImageUrl is valid URL", "AddRangeAsync called" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-ADD-06 | N | Existing vocab with valid Topic → linked to topic
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExistingVocabWithTopic_ShouldLinkToTopic()
        {
            // Arrange
            var dtos = new List<VocabularyExcelDTO> { new() { Text = "Cat", Definition = "Mèo" } };
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractVocabularyDataAsync(It.IsAny<IFormFile>())).ReturnsAsync(dtos);

            var existingDb = new List<Tokki.Domain.Entities.Vocabulary> { new() { VocabularyId = "V1", Text = "Cat", Definition = "Mèo" } };
            var mockVocabRepo = new Mock<IVocabularyRepository>();
            mockVocabRepo.Setup(x => x.GetExistingVocabEntitiesAsync(It.IsAny<List<(string Text, string Definition)>>()))
                         .ReturnsAsync(existingDb);

            var mockVocabTopicRepo = new Mock<IVocabularyTopicRepository>();
            mockVocabTopicRepo.Setup(x => x.GetVocabIdsByTopicIdAsync("T1"))
                              .ReturnsAsync(new List<string>());
            mockVocabTopicRepo.Setup(x => x.AddVocabulariesToTopicWithTransactionAsync("T1", It.IsAny<List<Tokki.Domain.Entities.Vocabulary>>(), "S1", It.IsAny<CancellationToken>()))
                              .ReturnsAsync((true, 1, new List<string>()));

            var command = new AddVocabByExcelCommand { File = GetMockFile(), TopicId = "T1", StaffId = "S1" };

            // Act
            var result = await CreateHandler(
                excelService: mockExcel,
                vocabRepo: mockVocabRepo,
                vocabTopicRepo: mockVocabTopicRepo
            ).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.LinkedExistingVocabList.Should().HaveCount(1);
            mockVocabTopicRepo.Verify(x => x.AddVocabulariesToTopicWithTransactionAsync("T1", It.IsAny<List<Tokki.Domain.Entities.Vocabulary>>(), "S1", It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Excel - Add Vocab", new TestCaseDetail
            {
                FunctionGroup     = "AddVocabByExcel",
                TestCaseID        = "TC-EXC-ADD-06",
                Description       = "Vocab exists in DB but not in topic, successfully links via transaction",
                ExpectedResult    = "Return 200, LinkedExistingVocabList = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "existingMatch != null", "TopicId != null", "!existingVocabIdsInTopic.Contains(VocabId)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-ADD-07 | A | Topic link transaction fails → catch returns 400
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TopicLinkTransactionFails_ShouldReturn400()
        {
            // Arrange
            var dtos = new List<VocabularyExcelDTO> { new() { Text = "Cat", Definition = "Mèo" } };
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractVocabularyDataAsync(It.IsAny<IFormFile>())).ReturnsAsync(dtos);

            var existingDb = new List<Tokki.Domain.Entities.Vocabulary> { new() { VocabularyId = "V1", Text = "Cat", Definition = "Mèo" } };
            var mockVocabRepo = new Mock<IVocabularyRepository>();
            mockVocabRepo.Setup(x => x.GetExistingVocabEntitiesAsync(It.IsAny<List<(string Text, string Definition)>>()))
                         .ReturnsAsync(existingDb);

            var mockVocabTopicRepo = new Mock<IVocabularyTopicRepository>();
            mockVocabTopicRepo.Setup(x => x.GetVocabIdsByTopicIdAsync("T1"))
                              .ReturnsAsync(new List<string>());
            // Transaction fails
            mockVocabTopicRepo.Setup(x => x.AddVocabulariesToTopicWithTransactionAsync("T1", It.IsAny<List<Tokki.Domain.Entities.Vocabulary>>(), "S1", It.IsAny<CancellationToken>()))
                              .ReturnsAsync((false, 0, new List<string> { "DB Error" }));

            var command = new AddVocabByExcelCommand { File = GetMockFile(), TopicId = "T1", StaffId = "S1" };

            // Act
            var result = await CreateHandler(
                excelService: mockExcel,
                vocabRepo: mockVocabRepo,
                vocabTopicRepo: mockVocabTopicRepo
            ).Handle(command, CancellationToken.None);

            // Assert
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Đã có lỗi hệ thống");

            // Excel Log
            QACollector.LogTestCase("Excel - Add Vocab", new TestCaseDetail
            {
                FunctionGroup     = "AddVocabByExcel",
                TestCaseID        = "TC-EXC-ADD-07",
                Description       = "Transaction fails, throws exception which is caught globally returning 400",
                ExpectedResult    = "Return 400 Status with rollback message",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "topicResult.Success == false => throw Exception => catch => 400" }
            });
        }
    }
}