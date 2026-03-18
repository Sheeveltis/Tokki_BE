using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.Commands.ImportQuestionsFromExcel;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class ImportQuestionsFromExcelCommandHandlerTests
    {
        private static IFormFile CreateFakeFile()
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(x => x.FileName).Returns("questions.xlsx");
            return mock.Object;
        }

        private ImportQuestionsFromExcelCommandHandler CreateHandler(
            Mock<IQuestionBankRepository>? qbRepo = null,
            Mock<IPassageRepository>? passageRepo = null,
            Mock<IExcelService>? excelService = null)
        {
            var mockQb = qbRepo ?? new Mock<IQuestionBankRepository>();

            // Default setup để tránh null exception
            mockQb.Setup(x => x.GetQuestionsByTypeAsync(It.IsAny<string>()))
                  .ReturnsAsync(new List<QuestionSignatureDTO>());
            mockQb.Setup(x => x.AddRangeAsync(
                        It.IsAny<IEnumerable<Domain.Entities.QuestionBank>>()))
                  .Returns(Task.CompletedTask);
            mockQb.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);

            return new ImportQuestionsFromExcelCommandHandler(
                mockQb.Object,
                (passageRepo ?? new Mock<IPassageRepository>()).Object,
                MockIdGeneratorService.GetMock().Object,
                (excelService ?? new Mock<IExcelService>()).Object,
                new Mock<ILogger<ImportQuestionsFromExcelCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_EmptyQuestionTypeId_ShouldReturn400()
        {
            var command = new ImportQuestionsFromExcelCommand
            {
                QuestionTypeId = "",
                ExcelFile = CreateFakeFile()
            };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup = "Import Questions From Excel",
                TestCaseID = "TC-IQE-01",
                Description = "Import với QuestionTypeId rỗng → return 400",
                ExpectedResult = "Return 400 Validation",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "QuestionTypeId = empty string",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_ExcelReadError_ShouldReturn400()
        {
            var command = new ImportQuestionsFromExcelCommand
            {
                QuestionTypeId = "QT-001",
                ExcelFile = CreateFakeFile()
            };

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractQuestionBankDataAsync(It.IsAny<IFormFile>()))
                     .ThrowsAsync(new Exception("Invalid Excel format"));

            var handler = CreateHandler(excelService: mockExcel);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup = "Import Questions From Excel",
                TestCaseID = "TC-IQE-02",
                Description = "Excel file không đọc được → return 400 Excel.ReadError",
                ExpectedResult = "Return 400 Excel.ReadError",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "ExtractQuestionBankDataAsync throws Exception",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidQuestions_ShouldImportAndReturn200()
        {
            var command = new ImportQuestionsFromExcelCommand
            {
                QuestionTypeId = "QT-001",
                ExcelFile = CreateFakeFile()
            };

            // ✅ Dùng đúng tên DTO: ExcelPassageDTO, ExcelQuestionDTO, ExcelOptionDTO
            var excelData = new QuestionBankImportDTO
            {
                Passages = new List<ExcelPassageDTO>(),
                Questions = new List<ExcelQuestionDTO>
                {
                    new ExcelQuestionDTO
                    {
                        RefId = "Q1",
                        Content = "Câu hỏi test",
                        Explanation = "Giải thích",
                        RowIndex = 2
                    }
                },
                Options = new List<ExcelOptionDTO>
                {
                    new ExcelOptionDTO
                    {
                        RefQuestionId = "Q1",
                        KeyOption = "A",
                        Content = "Đáp án A",
                        IsCorrectStr = "1",
                        RowIndex = 2
                    },
                    new ExcelOptionDTO
                    {
                        RefQuestionId = "Q1",
                        KeyOption = "B",
                        Content = "Đáp án B",
                        IsCorrectStr = "0",
                        RowIndex = 3
                    }
                }
            };

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExtractQuestionBankDataAsync(It.IsAny<IFormFile>()))
                     .ReturnsAsync(excelData);

            var mockQbRepo = new Mock<IQuestionBankRepository>();

            // ✅ GetQuestionsByTypeAsync trả về List<QuestionSignatureDTO>
            mockQbRepo.Setup(x => x.GetQuestionsByTypeAsync(It.IsAny<string>()))
                      .ReturnsAsync(new List<QuestionSignatureDTO>());

            // ✅ AddRangeAsync trả về Task (không phải Task<bool>)
            mockQbRepo.Setup(x => x.AddRangeAsync(
                        It.IsAny<IEnumerable<Domain.Entities.QuestionBank>>()))
                      .Returns(Task.CompletedTask);

            // ✅ SaveChangesAsync trả về Task<bool>
            mockQbRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

            var handler = CreateHandler(qbRepo: mockQbRepo, excelService: mockExcel);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.SuccessItems.Should().HaveCount(1);

            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup = "Import Questions From Excel",
                TestCaseID = "TC-IQE-03",
                Description = "Import 1 câu hỏi hợp lệ với đáp án đúng → SuccessItems.Count = 1",
                ExpectedResult = "Return 200, SuccessItems.Count = 1",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "1 valid question with correct answer (IsCorrectStr = '1')",
                    "No duplicate in DB",
                    "AddRangeAsync called",
                    "Return 200"
                }
            });
        }
    }
}