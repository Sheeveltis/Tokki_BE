using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Application.UseCases.Excel.Queries.ExportVocabByTopic;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class ExportVocabByTopicQueryHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static ExportVocabByTopicQueryHandler CreateHandler(
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null,
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IExcelService>? excelService = null)
        {
            return new ExportVocabByTopicQueryHandler(
                (vocabTopicRepo ?? new Mock<IVocabularyTopicRepository>()).Object,
                (topicRepo ?? new Mock<ITopicRepository>()).Object,
                (excelService ?? new Mock<IExcelService>()).Object);
        }

        private static List<VocabularyExportDTO> GetSampleVocabs() => new()
        {
            new VocabularyExportDTO { Text = "안녕하세요", Definition = "Xin chào" },
            new VocabularyExportDTO { Text = "감사합니다", Definition = "Cảm ơn" }
        };

        // ═══════════════════════════════════════════════════════════════════
        // ExportVocabByTopic_01 | A | Topic not found → 404 TopicNotFound
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturn404()
        {
            // Arrange
            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetTopicNameAsync("GHOST-TOPIC"))
                         .ReturnsAsync((string?)null);

            var query = new ExportVocabByTopicQuery { TopicId = "GHOST-TOPIC" };

            // Act
            var result = await CreateHandler(topicRepo: mockTopicRepo).Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Excel - Export Vocab Topic", new TestCaseDetail
            {
                FunctionGroup     = "ExportVocabByTopic",
                TestCaseID        = "ExportVocabByTopic_01",
                Description       = "TopicId does not exist → GetTopicNameAsync returns null",
                ExpectedResult    = "Return 404 TopicNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "string.IsNullOrEmpty(topicName)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ExportVocabByTopic_02 | A | Topic found but vocab list is null → VocabTopicIsEmpty
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabListNull_ShouldReturnVocabTopicIsEmpty()
        {
            // Arrange
            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetTopicNameAsync("T1")).ReturnsAsync("Korean Basics");

            var mockVocabTopicRepo = new Mock<IVocabularyTopicRepository>();
            mockVocabTopicRepo.Setup(x => x.GetVocabsByTopicIdAsync("T1"))
                              .ReturnsAsync((List<VocabularyExportDTO>?)null!);

            // Act
            var result = await CreateHandler(vocabTopicRepo: mockVocabTopicRepo, topicRepo: mockTopicRepo)
                             .Handle(new ExportVocabByTopicQuery { TopicId = "T1" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Excel - Export Vocab Topic", new TestCaseDetail
            {
                FunctionGroup     = "ExportVocabByTopic",
                TestCaseID        = "ExportVocabByTopic_02",
                Description       = "Topic exists but vocabs list is null",
                ExpectedResult    = "Return Failure VocabTopicIsEmpty",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "vocabs == null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ExportVocabByTopic_03 | A | Topic found but vocab list is empty → VocabTopicIsEmpty
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_VocabListEmpty_ShouldReturnVocabTopicIsEmpty()
        {
            // Arrange
            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetTopicNameAsync("T1")).ReturnsAsync("Korean Basics");

            var mockVocabTopicRepo = new Mock<IVocabularyTopicRepository>();
            mockVocabTopicRepo.Setup(x => x.GetVocabsByTopicIdAsync("T1"))
                              .ReturnsAsync(new List<VocabularyExportDTO>());

            // Act
            var result = await CreateHandler(vocabTopicRepo: mockVocabTopicRepo, topicRepo: mockTopicRepo)
                             .Handle(new ExportVocabByTopicQuery { TopicId = "T1" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Excel - Export Vocab Topic", new TestCaseDetail
            {
                FunctionGroup     = "ExportVocabByTopic",
                TestCaseID        = "ExportVocabByTopic_03",
                Description       = "Topic exists but has no vocabularies assigned",
                ExpectedResult    = "Return Failure VocabTopicIsEmpty",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!vocabs.Any()" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ExportVocabByTopic_04 | N | Valid topic with vocabs → exports file correctly
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidTopicWithVocabs_ShouldExportFile()
        {
            // Arrange
            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetTopicNameAsync("T1")).ReturnsAsync("Korean Basics");

            var mockVocabTopicRepo = new Mock<IVocabularyTopicRepository>();
            mockVocabTopicRepo.Setup(x => x.GetVocabsByTopicIdAsync("T1"))
                              .ReturnsAsync(GetSampleVocabs());

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExportVocabularyToExcelAsync(It.IsAny<List<VocabularyExportDTO>>(), It.IsAny<string>()))
                     .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 });

            // Act
            var result = await CreateHandler(
                vocabTopicRepo: mockVocabTopicRepo,
                topicRepo: mockTopicRepo,
                excelService: mockExcel
            ).Handle(new ExportVocabByTopicQuery { TopicId = "T1" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.FileName.Should().Be("Korean Basics.xlsx");
            result.Data.FileContent.Should().HaveCount(3);
            result.Data.ContentType.Should().Contain("spreadsheetml");

            // Excel Log
            QACollector.LogTestCase("Excel - Export Vocab Topic", new TestCaseDetail
            {
                FunctionGroup     = "ExportVocabByTopic",
                TestCaseID        = "ExportVocabByTopic_04",
                Description       = "Valid topic with 2 vocab items, file exported with correct name and content",
                ExpectedResult    = "Return 200 with FileName = 'Korean Basics.xlsx'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "topicName found", "vocabs.Count > 0", "ExportVocabularyToExcelAsync called" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ExportVocabByTopic_05 | N | Long topic name → truncated to 30 chars for sheet name
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_LongTopicName_ShouldTruncateSheetName()
        {
            // Arrange
            string longName = "This Is A Very Long Topic Name That Exceeds Thirty Characters";

            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetTopicNameAsync("T1")).ReturnsAsync(longName);

            var mockVocabTopicRepo = new Mock<IVocabularyTopicRepository>();
            mockVocabTopicRepo.Setup(x => x.GetVocabsByTopicIdAsync("T1")).ReturnsAsync(GetSampleVocabs());

            string? capturedSheetName = null;
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExportVocabularyToExcelAsync(It.IsAny<List<VocabularyExportDTO>>(), It.IsAny<string>()))
                     .Callback<List<VocabularyExportDTO>, string>((_, sheetName) => capturedSheetName = sheetName)
                     .ReturnsAsync(new byte[] { 0x01 });

            // Act
            var result = await CreateHandler(
                vocabTopicRepo: mockVocabTopicRepo,
                topicRepo: mockTopicRepo,
                excelService: mockExcel
            ).Handle(new ExportVocabByTopicQuery { TopicId = "T1" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedSheetName.Should().NotBeNull();
            capturedSheetName!.Length.Should().BeLessThanOrEqualTo(30);
            result.Data.FileName.Should().Be($"{longName}.xlsx"); // FileName uses full name

            // Excel Log
            QACollector.LogTestCase("Excel - Export Vocab Topic", new TestCaseDetail
            {
                FunctionGroup     = "ExportVocabByTopic",
                TestCaseID        = "ExportVocabByTopic_05",
                Description       = "Topic name longer than 30 chars → sheet name is truncated, filename uses full name",
                ExpectedResult    = "Sheet name ≤ 30 chars; FileName uses full topic name",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "topicName.Length > 30 ? topicName.Substring(0, 30) : topicName" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ExportVocabByTopic_06 | N | ContentType is set to openxml spreadsheet
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidTopic_ContentTypeShouldBeOpenXml()
        {
            // Arrange
            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetTopicNameAsync("T1")).ReturnsAsync("Korean");

            var mockVocabTopicRepo = new Mock<IVocabularyTopicRepository>();
            mockVocabTopicRepo.Setup(x => x.GetVocabsByTopicIdAsync("T1")).ReturnsAsync(GetSampleVocabs());

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExportVocabularyToExcelAsync(It.IsAny<List<VocabularyExportDTO>>(), It.IsAny<string>()))
                     .ReturnsAsync(new byte[] { 0x01 });

            // Act
            var result = await CreateHandler(
                vocabTopicRepo: mockVocabTopicRepo,
                topicRepo: mockTopicRepo,
                excelService: mockExcel
            ).Handle(new ExportVocabByTopicQuery { TopicId = "T1" }, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            // Excel Log
            QACollector.LogTestCase("Excel - Export Vocab Topic", new TestCaseDetail
            {
                FunctionGroup     = "ExportVocabByTopic",
                TestCaseID        = "ExportVocabByTopic_06",
                Description       = "ContentType is set to the correct MIME type for Excel files",
                ExpectedResult    = "ContentType = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ContentType hardcoded openxmlformat" }
            });
        }
    }
}