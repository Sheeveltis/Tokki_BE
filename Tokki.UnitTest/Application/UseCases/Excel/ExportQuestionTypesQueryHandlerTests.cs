using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Application.UseCases.Excel.Queries.ExportQuestionTypes;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class ExportQuestionTypesQueryHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static ExportQuestionTypesQueryHandler CreateHandler(
            Mock<IExcelBaseService>? excelService = null,
            Mock<IQuestionTypeRepository>? questionTypeRepo = null)
        {
            return new ExportQuestionTypesQueryHandler(
                (excelService ?? new Mock<IExcelBaseService>()).Object,
                (questionTypeRepo ?? new Mock<IQuestionTypeRepository>()).Object);
        }

        // ExportAsync has 3 parameters: data, sheetName, ignoredColumns (NO CancellationToken)
        private static void SetupExportAsync(Mock<IExcelBaseService> mock, byte[]? returnBytes = null)
        {
            mock.Setup(x => x.ExportAsync(
                    It.IsAny<IEnumerable<QuestionTypeExcelDTO>>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>?>()))
                .ReturnsAsync(returnBytes ?? new byte[] { 0x01 });
        }

        // ExamType enum: TopikI, TopikII, EntranceTestTopikI, EntranceTestTopikII, WeeklyAssessment
        private static QuestionType GetSampleQuestionType(string id = "QT-001") => new()
        {
            QuestionTypeId = id,
            Code           = "Q_RD_01",
            Name           = "Đọc hiểu 1",
            Description    = "Reading comprehension",
            ExamType       = ExamType.TopikI,
            Skill          = QuestionSkill.Reading,
            Difficulty     = DifficultyLevel.Easy,
            IsActive       = true,
            OrderIndex     = 1
        };

        // ═══════════════════════════════════════════════════════════════════
        // ExportQuestionTypes_01 | A | Repository throws → exception caught → failure
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturnExportError()
        {
            // Arrange
            var mockRepo = new Mock<IQuestionTypeRepository>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Database offline"));

            // Act
            var result = await CreateHandler(questionTypeRepo: mockRepo)
                             .Handle(new ExportQuestionTypesQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Database offline");

            // Excel Log
            QACollector.LogTestCase("Excel - Export Question Types", new TestCaseDetail
            {
                FunctionGroup     = "ExportQuestionTypes",
                TestCaseID        = "ExportQuestionTypes_01",
                Description       = "Repository throws exception during GetAllAsync",
                ExpectedResult    = "Return Failure EXPORT_ERROR",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception ex) => Failure EXPORT_ERROR" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ExportQuestionTypes_02 | A | Repository returns empty list → exports empty file
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyData_ShouldExportEmptyFile()
        {
            // Arrange
            var mockRepo = new Mock<IQuestionTypeRepository>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<QuestionType>());

            var mockExcel = new Mock<IExcelBaseService>();
            SetupExportAsync(mockExcel);

            // Act
            var result = await CreateHandler(excelService: mockExcel, questionTypeRepo: mockRepo)
                             .Handle(new ExportQuestionTypesQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.FileBytes.Should().NotBeEmpty();

            // Excel Log
            QACollector.LogTestCase("Excel - Export Question Types", new TestCaseDetail
            {
                FunctionGroup     = "ExportQuestionTypes",
                TestCaseID        = "ExportQuestionTypes_02",
                Description       = "Repository returns empty collection, proceeds to generate file with headers",
                ExpectedResult    = "Return 200 with FileBytes",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "data.Count == 0" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ExportQuestionTypes_03 | N | Valid data → properly mapped and file exported
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidData_ShouldMapAndExportCorrectly()
        {
            // Arrange
            var questionTypes = new List<QuestionType> { GetSampleQuestionType() };
            var mockRepo = new Mock<IQuestionTypeRepository>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(questionTypes);

            var mockExcel = new Mock<IExcelBaseService>();
            SetupExportAsync(mockExcel, new byte[] { 0x01, 0x02 });

            // Act
            var result = await CreateHandler(excelService: mockExcel, questionTypeRepo: mockRepo)
                             .Handle(new ExportQuestionTypesQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.FileName.Should().Contain("QuestionTypes_");
            result.Data.FileBytes.Should().NotBeEmpty();

            // Excel Log
            QACollector.LogTestCase("Excel - Export Question Types", new TestCaseDetail
            {
                FunctionGroup     = "ExportQuestionTypes",
                TestCaseID        = "ExportQuestionTypes_03",
                Description       = "Valid QuestionType list correctly mapped to DTO and exported",
                ExpectedResult    = "Return 200 with correct FileBytes and FileName",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "data.Select(q => new QuestionTypeExcelDTO)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ExportQuestionTypes_04 | N | Enum properties are converted to String properly
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidData_EnumPropertiesMappedAsStrings()
        {
            // Arrange
            var questionTypes = new List<QuestionType> { GetSampleQuestionType() };
            var mockRepo = new Mock<IQuestionTypeRepository>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(questionTypes);

            List<QuestionTypeExcelDTO>? capturedDtos = null;
            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ExportAsync(
                    It.IsAny<IEnumerable<QuestionTypeExcelDTO>>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>?>()))
                     .Callback<IEnumerable<QuestionTypeExcelDTO>, string, List<string>?>((dtos, _, _) => capturedDtos = dtos.ToList())
                     .ReturnsAsync(new byte[] { 0x01 });

            // Act
            var result = await CreateHandler(excelService: mockExcel, questionTypeRepo: mockRepo)
                             .Handle(new ExportQuestionTypesQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedDtos.Should().NotBeNull();
            capturedDtos![0].ExamType.Should().Be(ExamType.TopikI.ToString());
            capturedDtos![0].Skill.Should().Be(QuestionSkill.Reading.ToString());
            capturedDtos![0].Difficulty.Should().Be(DifficultyLevel.Easy.ToString());

            // Excel Log
            QACollector.LogTestCase("Excel - Export Question Types", new TestCaseDetail
            {
                FunctionGroup     = "ExportQuestionTypes",
                TestCaseID        = "ExportQuestionTypes_04",
                Description       = "Enum properties (ExamType, Skill, Difficulty) are converted to string via .ToString()",
                ExpectedResult    = "DTO enum fields equal enum name strings",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "q.ExamType.ToString()", "q.Skill.ToString()", "q.Difficulty.ToString()" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ExportQuestionTypes_05 | N | FileName contains QuestionTypes prefix and .xlsx
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidData_FileNameHasCorrectPrefix()
        {
            // Arrange
            var mockRepo = new Mock<IQuestionTypeRepository>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<QuestionType>());

            var mockExcel = new Mock<IExcelBaseService>();
            SetupExportAsync(mockExcel);

            // Act
            var result = await CreateHandler(excelService: mockExcel, questionTypeRepo: mockRepo)
                             .Handle(new ExportQuestionTypesQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.FileName.Should().StartWith("QuestionTypes_");
            result.Data.FileName.Should().EndWith(".xlsx");

            // Excel Log
            QACollector.LogTestCase("Excel - Export Question Types", new TestCaseDetail
            {
                FunctionGroup     = "ExportQuestionTypes",
                TestCaseID        = "ExportQuestionTypes_05",
                Description       = "FileName follows the QuestionTypes_{timestamp}.xlsx format",
                ExpectedResult    = "FileName starts with 'QuestionTypes_' and ends with '.xlsx'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "$\"QuestionTypes_{DateTime.Now:yyyyMMdd_HHmm}.xlsx\"" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ExportQuestionTypes_06 | A | ExcelBaseService throws → return EXPORT_ERROR
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExcelServiceThrows_ShouldReturnExportError()
        {
            // Arrange
            var mockRepo = new Mock<IQuestionTypeRepository>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<QuestionType>());

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ExportAsync(
                    It.IsAny<IEnumerable<QuestionTypeExcelDTO>>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>?>()))
                     .ThrowsAsync(new Exception("File locked"));

            // Act
            var result = await CreateHandler(excelService: mockExcel, questionTypeRepo: mockRepo)
                             .Handle(new ExportQuestionTypesQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("File locked");

            // Excel Log
            QACollector.LogTestCase("Excel - Export Question Types", new TestCaseDetail
            {
                FunctionGroup     = "ExportQuestionTypes",
                TestCaseID        = "ExportQuestionTypes_06",
                Description       = "ExcelBaseService throws exception during export",
                ExpectedResult    = "Return Failure EXPORT_ERROR with message",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception ex) => Failure('EXPORT_ERROR', ex.Message)" }
            });
        }
    }
}
