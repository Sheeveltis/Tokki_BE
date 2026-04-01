using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.Commands.ImportQuestionTypes;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class ImportQuestionTypesCommandHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static ImportQuestionTypesCommandHandler CreateHandler(
            Mock<IExcelBaseService>? excelService = null,
            Mock<IQuestionTypeRepository>? questionTypeRepo = null,
            Mock<IIdGeneratorService>? idGenerator = null)
        {
            return new ImportQuestionTypesCommandHandler(
                (excelService ?? new Mock<IExcelBaseService>()).Object,
                (questionTypeRepo ?? new Mock<IQuestionTypeRepository>()).Object,
                (idGenerator ?? new Mock<IIdGeneratorService>()).Object);
        }

        private static IFormFile GetMockFile()
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns("question_types.xlsx");
            return mock.Object;
        }

        private static ExcelImportResult<QuestionTypeExcelDTO> BuildExcelResult(
            List<QuestionTypeExcelDTO>? successItems = null,
            List<(int RowIndex, string Reason)>? errors = null)
        {
            var result = new ExcelImportResult<QuestionTypeExcelDTO>();
            int row = 2;
            foreach (var item in successItems ?? new List<QuestionTypeExcelDTO>())
            {
                result.SuccessItems.Add(new ExcelSuccessDetail<QuestionTypeExcelDTO> { RowIndex = row++, Data = item });
            }
            foreach (var (ri, reason) in errors ?? new List<(int, string)>())
            {
                result.Errors.Add(new ExcelErrorDetail { RowIndex = ri, Reason = reason });
            }
            return result;
        }

        private static QuestionTypeExcelDTO ValidDto(string code = "Q_RD_01") => new()
        {
            Code        = code,
            Name        = "Reading 1",
            Description = "Reading comprehension",
            ExamType    = "TOPIK1",
            Skill       = "Reading",
            Difficulty  = "Easy"
        };

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IMQT-01 | N | Excel format errors → appended to FailureList
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExcelFormatErrors_ShouldAppendToFailureList()
        {
            // Arrange
            var excelResult = BuildExcelResult(errors: new List<(int, string)> { (2, "Missing column") });

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ImportAsync<QuestionTypeExcelDTO>(It.IsAny<IFormFile>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(excelResult);

            var mockRepo = new Mock<IQuestionTypeRepository>();
            mockRepo.Setup(x => x.GetExistingCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<string>());
            mockRepo.Setup(x => x.GetMaxOrderIndexAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(0);

            // Act
            var result = await CreateHandler(excelService: mockExcel, questionTypeRepo: mockRepo)
                             .Handle(new ImportQuestionTypesCommand(GetMockFile()), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.FailureList.Should().HaveCount(1);
            result.Data.FailureList[0].Code.Should().Be("Lỗi định dạng");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Question Types", new TestCaseDetail
            {
                FunctionGroup     = "ImportQuestionTypes",
                TestCaseID        = "TC-EXC-IMQT-01",
                Description       = "Excel base service reports format error → added to FailureList",
                ExpectedResult    = "Return 200, FailureList = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "excelResult.Errors.Any()" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IMQT-02 | N | Missing required fields (Code or Name) → FailureList
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MissingCodeOrName_ShouldFailRow()
        {
            // Arrange
            var excelResult = BuildExcelResult(
                successItems: new List<QuestionTypeExcelDTO>
                {
                    new() { Code = null!, Name = null!, ExamType = "TOPIK1", Skill = "Reading", Difficulty = "Easy" }
                });

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ImportAsync<QuestionTypeExcelDTO>(It.IsAny<IFormFile>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(excelResult);

            var mockRepo = new Mock<IQuestionTypeRepository>();
            mockRepo.Setup(x => x.GetExistingCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<string>());
            mockRepo.Setup(x => x.GetMaxOrderIndexAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(0);

            // Act
            var result = await CreateHandler(excelService: mockExcel, questionTypeRepo: mockRepo)
                             .Handle(new ImportQuestionTypesCommand(GetMockFile()), CancellationToken.None);

            // Assert
            result.Data!.FailureList.Should().HaveCount(1);
            result.Data.FailureList[0].Reason.Should().Contain("Thiếu dữ liệu bắt buộc");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Question Types", new TestCaseDetail
            {
                FunctionGroup     = "ImportQuestionTypes",
                TestCaseID        = "TC-EXC-IMQT-02",
                Description       = "Row has null/empty Code or Name field",
                ExpectedResult    = "FailureList = 1 with 'Thiếu dữ liệu bắt buộc'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "string.IsNullOrWhiteSpace(item.Code) || string.IsNullOrWhiteSpace(item.Name)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IMQT-03 | N | Duplicate code in file → second occurrence FailureList
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicateCodeInFile_ShouldFailSecondRow()
        {
            // Arrange
            var excelResult = BuildExcelResult(
                successItems: new List<QuestionTypeExcelDTO>
                {
                    ValidDto("DUP_CODE"),
                    ValidDto("DUP_CODE") // duplicate
                });

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ImportAsync<QuestionTypeExcelDTO>(It.IsAny<IFormFile>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(excelResult);

            var mockRepo = new Mock<IQuestionTypeRepository>();
            mockRepo.Setup(x => x.GetExistingCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<string>());
            mockRepo.Setup(x => x.GetMaxOrderIndexAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionType>>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.Generate(10)).Returns("QT-ID-001");

            // Act
            var result = await CreateHandler(excelService: mockExcel, questionTypeRepo: mockRepo, idGenerator: mockIdGen)
                             .Handle(new ImportQuestionTypesCommand(GetMockFile()), CancellationToken.None);

            // Assert
            result.Data!.FailureList.Should().HaveCount(1);
            result.Data.FailureList[0].Reason.Should().Contain("bị trùng lặp trong file");
            result.Data.SuccessList.Should().HaveCount(1);

            // Excel Log
            QACollector.LogTestCase("Excel - Import Question Types", new TestCaseDetail
            {
                FunctionGroup     = "ImportQuestionTypes",
                TestCaseID        = "TC-EXC-IMQT-03",
                Description       = "Two rows with same Code → second fails processedCodesInFile HashSet",
                ExpectedResult    = "1 Success, 1 Failure with 'trùng lặp trong file'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!processedCodesInFile.Add(currentCode)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IMQT-04 | N | Code exists in DB → FailureList
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CodeExistsInDatabase_ShouldFailRow()
        {
            // Arrange
            var excelResult = BuildExcelResult(
                successItems: new List<QuestionTypeExcelDTO> { ValidDto("EXISTING_CODE") });

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ImportAsync<QuestionTypeExcelDTO>(It.IsAny<IFormFile>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(excelResult);

            var mockRepo = new Mock<IQuestionTypeRepository>();
            mockRepo.Setup(x => x.GetExistingCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<string> { "EXISTING_CODE" });
            mockRepo.Setup(x => x.GetMaxOrderIndexAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);

            // Act
            var result = await CreateHandler(excelService: mockExcel, questionTypeRepo: mockRepo)
                             .Handle(new ImportQuestionTypesCommand(GetMockFile()), CancellationToken.None);

            // Assert
            result.Data!.FailureList.Should().HaveCount(1);
            result.Data.FailureList[0].Reason.Should().Contain("đã tồn tại trong hệ thống");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Question Types", new TestCaseDetail
            {
                FunctionGroup     = "ImportQuestionTypes",
                TestCaseID        = "TC-EXC-IMQT-04",
                Description       = "Code already exists in database → existingDbCodesSet.Contains check fails row",
                ExpectedResult    = "FailureList = 1, 'đã tồn tại trong hệ thống'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "existingDbCodesSet.Contains(currentCode)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IMQT-05 | N | Valid row → entity created and inserted to DB
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRow_ShouldCreateEntityAndInsert()
        {
            // Arrange
            var excelResult = BuildExcelResult(successItems: new List<QuestionTypeExcelDTO> { ValidDto() });

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ImportAsync<QuestionTypeExcelDTO>(It.IsAny<IFormFile>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(excelResult);

            var mockRepo = new Mock<IQuestionTypeRepository>();
            mockRepo.Setup(x => x.GetExistingCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<string>());
            mockRepo.Setup(x => x.GetMaxOrderIndexAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionType>>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.Generate(10)).Returns("QT-001");

            // Act
            var result = await CreateHandler(excelService: mockExcel, questionTypeRepo: mockRepo, idGenerator: mockIdGen)
                             .Handle(new ImportQuestionTypesCommand(GetMockFile()), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.SuccessList.Should().HaveCount(1);
            mockRepo.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionType>>()), Times.Once);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Excel - Import Question Types", new TestCaseDetail
            {
                FunctionGroup     = "ImportQuestionTypes",
                TestCaseID        = "TC-EXC-IMQT-05",
                Description       = "Valid row passes all checks → entity created and bulk inserted to DB",
                ExpectedResult    = "Return 200, SuccessList = 1, AddRangeAsync + SaveChangesAsync called",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "newQuestionTypes.Any() => AddRangeAsync, SaveChangesAsync" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IMQT-06 | A | Database save throws → return DB_ERROR failure
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DatabaseThrowsException_ShouldReturnDbErrorFailure()
        {
            // Arrange
            var excelResult = BuildExcelResult(successItems: new List<QuestionTypeExcelDTO> { ValidDto() });

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ImportAsync<QuestionTypeExcelDTO>(It.IsAny<IFormFile>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(excelResult);

            var mockRepo = new Mock<IQuestionTypeRepository>();
            mockRepo.Setup(x => x.GetExistingCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<string>());
            mockRepo.Setup(x => x.GetMaxOrderIndexAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionType>>())).Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Constraint violation"));

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.Generate(10)).Returns("QT-001");

            // Act
            var result = await CreateHandler(excelService: mockExcel, questionTypeRepo: mockRepo, idGenerator: mockIdGen)
                             .Handle(new ImportQuestionTypesCommand(GetMockFile()), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Lỗi lưu database");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Question Types", new TestCaseDetail
            {
                FunctionGroup     = "ImportQuestionTypes",
                TestCaseID        = "TC-EXC-IMQT-06",
                Description       = "SaveChangesAsync throws exception → DB_ERROR failure returned",
                ExpectedResult    = "Return Failure DB_ERROR with exception message",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception ex) => Failure('DB_ERROR', ex.Message)" }
            });
        }
    }
}