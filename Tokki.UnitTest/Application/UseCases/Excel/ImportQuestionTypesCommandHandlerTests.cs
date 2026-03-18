using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Excel.Commands.ImportQuestionTypes;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class ImportQuestionTypesCommandHandlerTests
    {
        private static IFormFile CreateFakeFile()
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(x => x.FileName).Returns("question_types.xlsx");
            return mock.Object;
        }

        // ✅ Helper tạo mock excel với đúng signature: ImportAsync<T>(IFormFile, string?, CancellationToken)
        private static Mock<IExcelBaseService> SetupExcelMock(
            ExcelImportResult<QuestionTypeExcelDTO> result)
        {
            var mock = new Mock<IExcelBaseService>();
            mock.Setup(x => x.ImportAsync<QuestionTypeExcelDTO>(
                        It.IsAny<IFormFile>(),
                        It.IsAny<string?>(),        // ✅ string? không phải object
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
            return mock;
        }

        private ImportQuestionTypesCommandHandler CreateHandler(
            Mock<IExcelBaseService>? excelService = null,
            Mock<IQuestionTypeRepository>? qtRepo = null)
        {
            var mockQtRepo = qtRepo ?? new Mock<IQuestionTypeRepository>();

            // Default setup
            mockQtRepo.Setup(x => x.GetMaxOrderIndexAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(0);
            mockQtRepo.Setup(x => x.GetExistingCodesAsync(
                        It.IsAny<IEnumerable<string>>(),  // ✅ IEnumerable<string>
                        It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<string>());

            // ✅ AddRangeAsync trả về Task (void)
            mockQtRepo.Setup(x => x.AddRangeAsync(
                        It.IsAny<IEnumerable<Domain.Entities.QuestionType>>()))
                      .Returns(Task.CompletedTask);

            // ✅ SaveChangesAsync trả về Task<bool>
            mockQtRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

            return new ImportQuestionTypesCommandHandler(
                (excelService ?? new Mock<IExcelBaseService>()).Object,
                mockQtRepo.Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_MissingRequiredFields_ShouldAddToFailureList()
        {
            // ✅ Constructor nhận IFormFile
            var command = new ImportQuestionTypesCommand(CreateFakeFile());

            var excelResult = new ExcelImportResult<QuestionTypeExcelDTO>();
            excelResult.SuccessItems.Add(new ExcelSuccessDetail<QuestionTypeExcelDTO>
            {
                RowIndex = 2,
                Data = new QuestionTypeExcelDTO
                {
                    Code = "",      // thiếu Code → failure
                    Name = "Test",
                    ExamType = "TOEIC",
                    Skill = "Reading",
                    Difficulty = "Easy"
                }
            });

            var handler = CreateHandler(excelService: SetupExcelMock(excelResult));
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.FailureList.Should().HaveCount(1);
            result.Data.SuccessList.Should().BeEmpty();

            QACollector.LogTestCase("Excel - Import Question Types", new TestCaseDetail
            {
                FunctionGroup = "Import Question Types",
                TestCaseID = "TC-IQTP-01",
                Description = "Import row thiếu Code → vào FailureList",
                ExpectedResult = "FailureList.Count = 1, SuccessList = empty",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Code = empty string",
                    "Row thiếu dữ liệu bắt buộc",
                    "FailureList.Count = 1"
                }
            });
        }

        [Fact]
        public async Task Handle_DuplicateCodeInFile_ShouldAddSecondToFailureList()
        {
            var command = new ImportQuestionTypesCommand(CreateFakeFile());

            var excelResult = new ExcelImportResult<QuestionTypeExcelDTO>();
            excelResult.SuccessItems.Add(new ExcelSuccessDetail<QuestionTypeExcelDTO>
            {
                RowIndex = 2,
                Data = new QuestionTypeExcelDTO
                {
                    Code = "QT-001",
                    Name = "Question Type 1",
                    ExamType = "TOEIC",
                    Skill = "Reading",
                    Difficulty = "Easy"
                }
            });
            excelResult.SuccessItems.Add(new ExcelSuccessDetail<QuestionTypeExcelDTO>
            {
                RowIndex = 3,
                Data = new QuestionTypeExcelDTO
                {
                    Code = "QT-001", // trùng
                    Name = "Question Type Duplicate",
                    ExamType = "TOEIC",
                    Skill = "Reading",
                    Difficulty = "Easy"
                }
            });

            var mockQtRepo = new Mock<IQuestionTypeRepository>();
            mockQtRepo.Setup(x => x.GetExistingCodesAsync(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<string>()); // không có trong DB
            mockQtRepo.Setup(x => x.GetMaxOrderIndexAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(0);
            mockQtRepo.Setup(x => x.AddRangeAsync(
                        It.IsAny<IEnumerable<Domain.Entities.QuestionType>>()))
                      .Returns(Task.CompletedTask);
            mockQtRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

            var handler = CreateHandler(
                excelService: SetupExcelMock(excelResult),
                qtRepo: mockQtRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.SuccessList.Should().HaveCount(1);
            result.Data.FailureList.Should().HaveCount(1);
            result.Data.FailureList[0].Reason.Should().Contain("trùng lặp");

            QACollector.LogTestCase("Excel - Import Question Types", new TestCaseDetail
            {
                FunctionGroup = "Import Question Types",
                TestCaseID = "TC-IQTP-02",
                Description = "2 rows cùng Code trong file → row 2 vào FailureList",
                ExpectedResult = "SuccessList.Count = 1, FailureList.Count = 1",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "2 rows cùng Code (boundary: duplicate trong file)",
                    "1 success, 1 failure",
                    "Return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_CodeExistsInDb_ShouldAddToFailureList()
        {
            var command = new ImportQuestionTypesCommand(CreateFakeFile());

            var excelResult = new ExcelImportResult<QuestionTypeExcelDTO>();
            excelResult.SuccessItems.Add(new ExcelSuccessDetail<QuestionTypeExcelDTO>
            {
                RowIndex = 2,
                Data = new QuestionTypeExcelDTO
                {
                    Code = "QT-EXISTS",
                    Name = "Existing Type",
                    ExamType = "TOEIC",
                    Skill = "Reading",
                    Difficulty = "Easy"
                }
            });

            var mockQtRepo = new Mock<IQuestionTypeRepository>();
            mockQtRepo.Setup(x => x.GetExistingCodesAsync(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<string> { "QT-EXISTS" }); // đã có trong DB
            mockQtRepo.Setup(x => x.GetMaxOrderIndexAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(5);

            var handler = CreateHandler(
                excelService: SetupExcelMock(excelResult),
                qtRepo: mockQtRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.FailureList.Should().HaveCount(1);
            result.Data.SuccessList.Should().BeEmpty();

            QACollector.LogTestCase("Excel - Import Question Types", new TestCaseDetail
            {
                FunctionGroup = "Import Question Types",
                TestCaseID = "TC-IQTP-03",
                Description = "Code đã tồn tại trong DB → vào FailureList",
                ExpectedResult = "FailureList.Count = 1, SuccessList = empty",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Code tồn tại trong DB",
                    "FailureList.Count = 1"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidData_ShouldReturn200WithCorrectOrderIndex()
        {
            var command = new ImportQuestionTypesCommand(CreateFakeFile());

            var excelResult = new ExcelImportResult<QuestionTypeExcelDTO>();
            excelResult.SuccessItems.Add(new ExcelSuccessDetail<QuestionTypeExcelDTO>
            {
                RowIndex = 2,
                Data = new QuestionTypeExcelDTO
                {
                    Code = "QT-NEW",
                    Name = "New Question Type",
                    ExamType = "TOEIC",
                    Skill = "Reading",
                    Difficulty = "Easy"
                }
            });

            Domain.Entities.QuestionType? capturedEntity = null;

            var mockQtRepo = new Mock<IQuestionTypeRepository>();
            mockQtRepo.Setup(x => x.GetExistingCodesAsync(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<string>());
            mockQtRepo.Setup(x => x.GetMaxOrderIndexAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(3); // maxOrderIndex = 3 → new = 4
            mockQtRepo.Setup(x => x.AddRangeAsync(
                        It.IsAny<IEnumerable<Domain.Entities.QuestionType>>()))
                      .Callback<IEnumerable<Domain.Entities.QuestionType>>(list =>
                          capturedEntity = list.FirstOrDefault())
                      .Returns(Task.CompletedTask);
            mockQtRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

            var handler = CreateHandler(
                excelService: SetupExcelMock(excelResult),
                qtRepo: mockQtRepo);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.SuccessList.Should().HaveCount(1);

            capturedEntity.Should().NotBeNull();
            capturedEntity!.OrderIndex.Should().Be(4); // maxOrderIndex(3) + 1

            QACollector.LogTestCase("Excel - Import Question Types", new TestCaseDetail
            {
                FunctionGroup = "Import Question Types",
                TestCaseID = "TC-IQTP-04",
                Description = "Import 1 QuestionType hợp lệ → OrderIndex = maxOrderIndex + 1",
                ExpectedResult = "Return 200, SuccessList.Count = 1, OrderIndex = 4",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid Code, Name, ExamType, Skill, Difficulty",
                    "MaxOrderIndex = 3 → NewOrderIndex = 4",
                    "AddRangeAsync called",
                    "Return 200"
                }
            });
        }
    }
}