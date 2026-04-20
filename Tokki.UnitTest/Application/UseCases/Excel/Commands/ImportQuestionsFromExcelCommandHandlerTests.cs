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
using Tokki.Application.UseCases.Excel.Commands.ImportQuestionsFromExcel;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel.Commands
{
    public class ImportQuestionsFromExcelCommandHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _qbMock = new();
        private readonly Mock<IPassageRepository> _passageMock = new();
        private readonly Mock<IIdGeneratorService> _idMock = new();
        private readonly Mock<IExcelService> _excelMock = new();
        private readonly Mock<ILogger<ImportQuestionsFromExcelCommandHandler>> _loggerMock = new();

        private ImportQuestionsFromExcelCommandHandler CreateHandler()
        {
            return new ImportQuestionsFromExcelCommandHandler(
                _qbMock.Object, _passageMock.Object, _idMock.Object, _excelMock.Object, _loggerMock.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // ImportQuestionsFromExcelCommandHandler_01 | A | Error QuestionTypeId Missing
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_QuestionTypeIdMissing_ShouldReturn400()
        {
            var handler = CreateHandler();
            var result = await handler.Handle(new ImportQuestionsFromExcelCommand { QuestionTypeId = "" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors[0].Description.Should().Contain("QuestionTypeId không được để trống");

            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup = "ImportQuestionsFromExcelCommandHandler",
                TestCaseID = "ImportQuestionsFromExcelCommandHandler_01",
                Description = "Validation immediately denies operations incorrectly bound mapping type empty",
                ExpectedResult = "Return 400 Warning",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionTypeId is empty wrapper validation string error" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // ImportQuestionsFromExcelCommandHandler_02 | A | Exception Parsing Excel data Extraction
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExtractFailure_ShouldReturn400()
        {
            var mockFile = new Mock<IFormFile>();
            _excelMock.Setup(x => x.ExtractQuestionBankDataAsync(mockFile.Object)).ThrowsAsync(new Exception("Fail format"));

            var handler = CreateHandler();
            var result = await handler.Handle(new ImportQuestionsFromExcelCommand { QuestionTypeId = "qType", ExcelFile = mockFile.Object }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors[0].Code.Should().Be("Excel.ReadError");

            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup = "ImportQuestionsFromExcelCommandHandler",
                TestCaseID = "ImportQuestionsFromExcelCommandHandler_02",
                Description = "Exceptions parsing excel internal EPPlus wrappers propagate gracefully effectively safe",
                ExpectedResult = "Return 400 safely extracted from block throws logic",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Extraction service triggers exception explicitly internal method" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // ImportQuestionsFromExcelCommandHandler_03 | A | Invalid Options Missing IsCorrect Flag Format
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_InvalidIsCorrect_ShouldAddToErrorsListOnly()
        {
            var mockFile = new Mock<IFormFile>();
            var data = new QuestionBankImportDTO();
            data.Questions.Add(new ExcelQuestionDTO { RowIndex = 1, RefId = "q1", Content = "Q Content", Explanation = "Exp" });
            data.Options.Add(new ExcelOptionDTO { RowIndex = 2, RefQuestionId = "q1", IsCorrectStr = "ABC" }); // Invalid flag

            _excelMock.Setup(x => x.ExtractQuestionBankDataAsync(mockFile.Object)).ReturnsAsync(data);
            _qbMock.Setup(x => x.GetQuestionsByTypeAsync("qType")).ReturnsAsync(new List<Tokki.Application.UseCases.QuestionBanks.DTOs.QuestionSignatureDTO>());

            var handler = CreateHandler();
            var result = await handler.Handle(new ImportQuestionsFromExcelCommand { QuestionTypeId = "qType", ExcelFile = mockFile.Object }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue(); // Overall it didn't throw exception, just added to failed list!
            result.Data!.TotalFailed.Should().Be(1);
            result.Data.Errors[0].ErrorReason.Should().Contain("sai format");

            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup = "ImportQuestionsFromExcelCommandHandler",
                TestCaseID = "ImportQuestionsFromExcelCommandHandler_03",
                Description = "Invalid 'IsCorrect' format identifies mapping issue properly recording inside Error Log reporting list object dynamically securely.",
                ExpectedResult = "Failed row count increases safely",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Option IsCorrect != 0 && != 1" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // ImportQuestionsFromExcelCommandHandler_04 | A | Trắc nghiệm nhưng thiếu đáp án Đúng
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NoCorrectOption_ShouldAddToErrorsList()
        {
            var mockFile = new Mock<IFormFile>();
            var data = new QuestionBankImportDTO();
            data.Questions.Add(new ExcelQuestionDTO { RowIndex = 1, RefId = "q1", Content = "Q", Explanation = "E" });
            data.Options.Add(new ExcelOptionDTO { RowIndex = 2, RefQuestionId = "q1", IsCorrectStr = "0" }); 
            data.Options.Add(new ExcelOptionDTO { RowIndex = 3, RefQuestionId = "q1", IsCorrectStr = "0" }); 

            _excelMock.Setup(x => x.ExtractQuestionBankDataAsync(mockFile.Object)).ReturnsAsync(data);
            _qbMock.Setup(x => x.GetQuestionsByTypeAsync("qType")).ReturnsAsync(new List<Tokki.Application.UseCases.QuestionBanks.DTOs.QuestionSignatureDTO>());

            var handler = CreateHandler();
            var result = await handler.Handle(new ImportQuestionsFromExcelCommand { QuestionTypeId = "qType", ExcelFile = mockFile.Object }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue(); 
            result.Data!.TotalFailed.Should().Be(1);
            result.Data.Errors[0].ErrorReason.Should().Contain("không có đáp án đúng nào");

            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup = "ImportQuestionsFromExcelCommandHandler",
                TestCaseID = "ImportQuestionsFromExcelCommandHandler_04",
                Description = "Multiple questions with all 0 flag correctly rejected effectively",
                ExpectedResult = "Failed row correctly registered safely",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No option has '1' mapping strings properly" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // ImportQuestionsFromExcelCommandHandler_05 | A | Duplicated Database Content String matched -> Errors List
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicatedDBData_ShouldTriggerDuplicateError()
        {
            var mockFile = new Mock<IFormFile>();
            var data = new QuestionBankImportDTO();
            data.Questions.Add(new ExcelQuestionDTO { RowIndex = 1, RefId = "q1", Content = "Same Text", Explanation = "E" });

            _excelMock.Setup(x => x.ExtractQuestionBankDataAsync(mockFile.Object)).ReturnsAsync(data);
            
            // Matches signature
            var existing = new List<Tokki.Application.UseCases.QuestionBanks.DTOs.QuestionSignatureDTO>
            {
                new Tokki.Application.UseCases.QuestionBanks.DTOs.QuestionSignatureDTO { Content = "Same Text", MediaUrl = "", OptionContents = new List<string>() }
            };
            _qbMock.Setup(x => x.GetQuestionsByTypeAsync("qType")).ReturnsAsync(existing);

            var handler = CreateHandler();
            var result = await handler.Handle(new ImportQuestionsFromExcelCommand { QuestionTypeId = "qType", ExcelFile = mockFile.Object }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue(); 
            result.Data!.TotalFailed.Should().Be(1);
            result.Data.Errors[0].ErrorReason.Should().Contain("đã tồn tại trong DB");

            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup = "ImportQuestionsFromExcelCommandHandler",
                TestCaseID = "ImportQuestionsFromExcelCommandHandler_05",
                Description = "Identical matching duplicates from repo trigger early termination via Signature Generator logic securely",
                ExpectedResult = "Identifies duplicated elements inside hashset properly",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DB content Signature identical" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // ImportQuestionsFromExcelCommandHandler_06 | N | Success Path Mapping Passage and DB insertion perfectly
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SuccessValidMapping_ShouldInsertEffectively()
        {
            var mockFile = new Mock<IFormFile>();
            var data = new QuestionBankImportDTO();
            data.Passages.Add(new ExcelPassageDTO { RefId = "p1", Title = "TitlePassage", MediaType = "Image" });
            
            data.Questions.Add(new ExcelQuestionDTO { RowIndex = 1, RefId = "q1", Content = "Question Safe", Explanation = "Exp", RefPassageId = "p1" });
            data.Options.Add(new ExcelOptionDTO { RowIndex = 2, RefQuestionId = "q1", IsCorrectStr = "1", Content = "Safe Ans" }); 

            _excelMock.Setup(x => x.ExtractQuestionBankDataAsync(mockFile.Object)).ReturnsAsync(data);
            _qbMock.Setup(x => x.GetQuestionsByTypeAsync("qType")).ReturnsAsync(new List<Tokki.Application.UseCases.QuestionBanks.DTOs.QuestionSignatureDTO>());
            _idMock.Setup(x => x.GenerateCustom(10)).Returns("ok-id");

            var handler = CreateHandler();
            var result = await handler.Handle(new ImportQuestionsFromExcelCommand { QuestionTypeId = "qType", ExcelFile = mockFile.Object }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue(); 
            result.Data!.TotalSuccess.Should().Be(1);
            
            _passageMock.Verify(x => x.AddRangeAsync(It.Is<List<Domain.Entities.Passage>>(l => l.Count == 1 && l[0].Title == "TitlePassage")), Times.Once);
            _qbMock.Verify(x => x.AddRangeAsync(It.Is<List<Domain.Entities.QuestionBank>>(l => l.Count == 1 && l[0].Content == "Question Safe")), Times.Once);

            QACollector.LogTestCase("Excel - Import Questions", new TestCaseDetail
            {
                FunctionGroup = "ImportQuestionsFromExcelCommandHandler",
                TestCaseID = "ImportQuestionsFromExcelCommandHandler_06",
                Description = "Safe formatting mappings and links accurately tie passages into entity database requests effectively",
                ExpectedResult = "Success element added, passage instantiated appropriately dynamically",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Successfully generated mappings efficiently" }
            });
        }
    }
}
