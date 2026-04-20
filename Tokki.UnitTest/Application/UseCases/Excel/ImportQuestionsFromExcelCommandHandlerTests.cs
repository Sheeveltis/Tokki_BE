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
using Tokki.Application.UseCases.Excel.Commands.ImportQuestionsFromExcel;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class ImportQuestionsFromExcelCommandHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static ImportQuestionsFromExcelCommandHandler CreateHandler(
            Mock<IQuestionBankRepository>? questionRepo = null,
            Mock<IPassageRepository>? passageRepo = null,
            Mock<IIdGeneratorService>? idGenerator = null,
            Mock<IExcelService>? excelService = null)
        {
            return new ImportQuestionsFromExcelCommandHandler(
                (questionRepo ?? new Mock<IQuestionBankRepository>()).Object,
                (passageRepo ?? new Mock<IPassageRepository>()).Object,
                (idGenerator ?? new Mock<IIdGeneratorService>()).Object,
                (excelService ?? new Mock<IExcelService>()).Object,
                new Mock<ILogger<ImportQuestionsFromExcelCommandHandler>>().Object);
        }

        private static IFormFile GetMockFile()
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns("questions.xlsx");
            return mock.Object;
        }

        private static QuestionBankImportDTO GetMinimalValidImportData() => new()
        {
            Passages  = new List<ExcelPassageDTO>(),
            Questions = new List<ExcelQuestionDTO>
            {
                new() { RowIndex = 2, RefId = "Q_001", Content = "What is 가?", Explanation = "The letter 가", MediaUrl = null, RefPassageId = null }
            },
            Options = new List<ExcelOptionDTO>
            {
                new() { RowIndex = 2, RefId = "O_001", RefQuestionId = "Q_001", KeyOption = "A", Content = "가 sound", IsCorrectStr = "1" },
                new() { RowIndex = 3, RefId = "O_002", RefQuestionId = "Q_001", KeyOption = "B", Content = "나 sound", IsCorrectStr = "0" }
            }
        };

        // ═══════════════════════════════════════════════════════════════════
        // ImportQuestionsFromExcel_01 | B | QuestionTypeId is empty → Validation 400
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyQuestionTypeId_ShouldReturn400Validation()
        {
            // Arrange
            var command = new ImportQuestionsFromExcelCommand { QuestionTypeId = "   ", ExcelFile = GetMockFile() };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("QuestionTypeId");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup     = "ImportQuestionsFromExcel",
                TestCaseID        = "ImportQuestionsFromExcel_01",
                Description       = "QuestionTypeId is whitespace/empty → validation fail 400",
                ExpectedResult    = "Return 400 Validation error",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "string.IsNullOrEmpty(cleanQuestionTypeId)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ImportQuestionsFromExcel_02 | A | Excel parsing throws → return 400 ReadError
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExcelParsingThrows_ShouldReturn400ReadError()
        {
            // Arrange
            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractQuestionBankDataAsync(It.IsAny<IFormFile>()))
                     .ThrowsAsync(new Exception("File corrupted"));

            var command = new ImportQuestionsFromExcelCommand { QuestionTypeId = "QT-001", ExcelFile = GetMockFile() };

            // Act
            var result = await CreateHandler(excelService: mockExcel).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("File corrupted");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup     = "ImportQuestionsFromExcel",
                TestCaseID        = "ImportQuestionsFromExcel_02",
                Description       = "ExtractQuestionBankDataAsync throws exception",
                ExpectedResult    = "Return 400 Excel.ReadError with exception message",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception ex) around ExtractQuestionBankDataAsync" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ImportQuestionsFromExcel_03 | A | Question has invalid IsCorrect format → added to Errors
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_InvalidIsCorrectFormat_ShouldAddToErrors()
        {
            // Arrange
            var badData = new QuestionBankImportDTO
            {
                Passages  = new List<ExcelPassageDTO>(),
                Questions = new List<ExcelQuestionDTO>
                {
                    new() { RowIndex = 2, RefId = "Q_001", Content = "Question?", Explanation = "Explain", MediaUrl = null }
                },
                Options = new List<ExcelOptionDTO>
                {
                    new() { RowIndex = 2, RefId = "O_001", RefQuestionId = "Q_001", KeyOption = "A", Content = "Opt A", IsCorrectStr = "YES" } // invalid format
                }
            };

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractQuestionBankDataAsync(It.IsAny<IFormFile>())).ReturnsAsync(badData);

            var mockQuestionRepo = new Mock<IQuestionBankRepository>();
            mockQuestionRepo.Setup(x => x.GetQuestionsByTypeAsync(It.IsAny<string>()))
                             .ReturnsAsync(new List<QuestionSignatureDTO>());
            mockQuestionRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new ImportQuestionsFromExcelCommand { QuestionTypeId = "QT-001", ExcelFile = GetMockFile() };

            // Act
            var result = await CreateHandler(questionRepo: mockQuestionRepo, excelService: mockExcel).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Errors.Should().HaveCount(1);
            result.Data.Errors[0].ErrorReason.Should().Contain("sai format");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup     = "ImportQuestionsFromExcel",
                TestCaseID        = "ImportQuestionsFromExcel_03",
                Description       = "Option IsCorrect field has invalid value ('YES' instead of '0'/'1')",
                ExpectedResult    = "Return 200, Errors list has 1 item with 'sai format'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "invalidOption.IsCorrectStr != '0' && != '1'" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ImportQuestionsFromExcel_04 | A | Question has options but no correct answer → added to Errors
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_HasOptionsButNoCorrectAnswer_ShouldAddToErrors()
        {
            // Arrange
            var badData = new QuestionBankImportDTO
            {
                Passages  = new List<ExcelPassageDTO>(),
                Questions = new List<ExcelQuestionDTO>
                {
                    new() { RowIndex = 2, RefId = "Q_001", Content = "Question?", Explanation = "Explain", MediaUrl = null }
                },
                Options = new List<ExcelOptionDTO>
                {
                    new() { RowIndex = 2, RefId = "O_001", RefQuestionId = "Q_001", KeyOption = "A", Content = "Opt A", IsCorrectStr = "0" }, // no correct
                    new() { RowIndex = 3, RefId = "O_002", RefQuestionId = "Q_001", KeyOption = "B", Content = "Opt B", IsCorrectStr = "0" }
                }
            };

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractQuestionBankDataAsync(It.IsAny<IFormFile>())).ReturnsAsync(badData);

            var mockQuestionRepo = new Mock<IQuestionBankRepository>();
            mockQuestionRepo.Setup(x => x.GetQuestionsByTypeAsync(It.IsAny<string>()))
                             .ReturnsAsync(new List<QuestionSignatureDTO>());
            mockQuestionRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new ImportQuestionsFromExcelCommand { QuestionTypeId = "QT-001", ExcelFile = GetMockFile() };

            // Act
            var result = await CreateHandler(questionRepo: mockQuestionRepo, excelService: mockExcel).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Errors.Should().HaveCount(1);
            result.Data.Errors[0].ErrorReason.Should().Contain("không có đáp án đúng");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup     = "ImportQuestionsFromExcel",
                TestCaseID        = "ImportQuestionsFromExcel_04",
                Description       = "Multiple choice question has no correct answer (all IsCorrect = 0)",
                ExpectedResult    = "Return 200, Errors has 1 item 'không có đáp án đúng'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "hasOptions && !hasCorrectAnswer" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ImportQuestionsFromExcel_05 | N | Valid question → inserted to DB, SuccessItems populated
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidQuestion_ShouldInsertAndReturnSuccess()
        {
            // Arrange
            var validData = GetMinimalValidImportData();

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractQuestionBankDataAsync(It.IsAny<IFormFile>())).ReturnsAsync(validData);

            var mockQuestionRepo = new Mock<IQuestionBankRepository>();
            mockQuestionRepo.Setup(x => x.GetQuestionsByTypeAsync(It.IsAny<string>()))
                             .ReturnsAsync(new List<QuestionSignatureDTO>());
            mockQuestionRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Domain.Entities.QuestionBank>>()))
                             .Returns(Task.CompletedTask);
            mockQuestionRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                             .ReturnsAsync(true);

            var mockPassageRepo = new Mock<IPassageRepository>();
            mockPassageRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Domain.Entities.Passage>>()))
                           .Returns(Task.CompletedTask);

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("QID-001");

            var command = new ImportQuestionsFromExcelCommand { QuestionTypeId = "QT-001", ExcelFile = GetMockFile() };

            // Act
            var result = await CreateHandler(
                questionRepo: mockQuestionRepo,
                passageRepo: mockPassageRepo,
                idGenerator: mockIdGen,
                excelService: mockExcel
            ).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.SuccessItems.Should().HaveCount(1);
            result.Data.Errors.Should().BeEmpty();

            // Excel Log
            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup     = "ImportQuestionsFromExcel",
                TestCaseID        = "ImportQuestionsFromExcel_05",
                Description       = "Valid question with correct option is inserted to DB successfully",
                ExpectedResult    = "Return 200, SuccessItems = 1, Errors = 0",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "question passes all validations", "AddRangeAsync called", "SaveChangesAsync called" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // ImportQuestionsFromExcel_06 | A | Duplicate question in DB → added to Errors
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicateQuestionInDb_ShouldAddToErrors()
        {
            // Arrange
            var validData = GetMinimalValidImportData();

            // Simulate existing question with same content/options signature
            var existingQ = new QuestionSignatureDTO
            {
                Content        = "What is 가?",
                MediaUrl       = null,
                OptionContents = new List<string> { "가 sound", "나 sound" }
            };

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractQuestionBankDataAsync(It.IsAny<IFormFile>())).ReturnsAsync(validData);

            var mockQuestionRepo = new Mock<IQuestionBankRepository>();
            mockQuestionRepo.Setup(x => x.GetQuestionsByTypeAsync(It.IsAny<string>()))
                             .ReturnsAsync(new List<QuestionSignatureDTO> { existingQ });
            mockQuestionRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new ImportQuestionsFromExcelCommand { QuestionTypeId = "QT-001", ExcelFile = GetMockFile() };

            // Act
            var result = await CreateHandler(questionRepo: mockQuestionRepo, excelService: mockExcel).Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Errors.Should().HaveCount(1);
            result.Data.Errors[0].ErrorReason.Should().Contain("Duplicate");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup     = "ImportQuestionsFromExcel",
                TestCaseID        = "ImportQuestionsFromExcel_06",
                Description       = "Question signature matches an existing record in DB → marked as Duplicate",
                ExpectedResult    = "Return 200, Errors = 1 with 'Duplicate' reason",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "existingSignatures.Contains(currentSignature)" }
            });
        }
    }
}