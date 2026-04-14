using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Titles.Commands.ImportTitles;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;
using Microsoft.AspNetCore.Http.Internal;

namespace Tokki.UnitTest.Application.UseCases.Titles.Commands
{
    public class ImportTitlesCommandHandlerTests
    {
        private readonly Mock<ITitleRepository> _mockRepo;
        private readonly Mock<IExcelService> _mockExcel;
        private readonly Mock<IIdGeneratorService> _mockIdGen;
        private readonly ImportTitlesCommandHandler _handler;

        public ImportTitlesCommandHandlerTests()
        {
            _mockRepo = new Mock<ITitleRepository>();
            _mockExcel = new Mock<IExcelService>();
            _mockIdGen = new Mock<IIdGeneratorService>();
            _handler = new ImportTitlesCommandHandler(_mockRepo.Object, _mockExcel.Object, _mockIdGen.Object);
        }

        private IFormFile CreateMockFile()
        {
            var content = "dummy-content";
            var fileName = "test.xlsx";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            return new FormFile(stream, 0, stream.Length, "id_from_form", fileName);
        }

        [Fact]
        public async Task Handle_FileNull_ReturnsFailure()
        {
            var result = await _handler.Handle(new ImportTitlesCommand { File = null }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("File không hợp lệ.");

            QACollector.LogTestCase("Title - Import", new TestCaseDetail
            {
                FunctionGroup     = "ImportTitlesCommandHandler",
                TestCaseID        = "TC-TTL-IMT-01",
                Description       = "File is null",
                ExpectedResult    = "Failure message 'File không hợp lệ.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "File=null" }
            });
        }

        [Fact]
        public async Task Handle_ExcelDataEmpty_ReturnsFailure()
        {
            var file = CreateMockFile();
            _mockExcel.Setup(x => x.ExtractTitleDataAsync(file)).ReturnsAsync(new List<TitleExcelDTO>());

            var result = await _handler.Handle(new ImportTitlesCommand { File = file }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Không có dữ liệu trong file.");

            QACollector.LogTestCase("Title - Import", new TestCaseDetail
            {
                FunctionGroup     = "ImportTitlesCommandHandler",
                TestCaseID        = "TC-TTL-IMT-02",
                Description       = "Excel parsed data is empty",
                ExpectedResult    = "Failure message 'Không có dữ liệu trong file.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Returns empty array" }
            });
        }

        [Fact]
        public async Task Handle_ValidationErrors_ReturnsFailureSummary()
        {
            var file = CreateMockFile();
            var dtos = new List<TitleExcelDTO>
            {
                new TitleExcelDTO { Name = "", Description = "desc", ColorHex = "xyz", IconUrl = "url", RequirementType = "InvalidType", RequirementQuantity = -5 }
            };
            _mockExcel.Setup(x => x.ExtractTitleDataAsync(file)).ReturnsAsync(dtos);

            var result = await _handler.Handle(new ImportTitlesCommand { File = file }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Tên danh hiệu không được để trống.");
            result.Message.Should().Contain("không đúng định dạng HEX");
            result.Message.Should().Contain("không hợp lệ (Hỗ trợ:");
            result.Message.Should().Contain("không được âm.");

            QACollector.LogTestCase("Title - Import", new TestCaseDetail
            {
                FunctionGroup     = "ImportTitlesCommandHandler",
                TestCaseID        = "TC-TTL-IMT-03",
                Description       = "Invalid data rows testing mapping logic",
                ExpectedResult    = "Returns 400 failure with error blocks",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Multiple validation errors concatenated" }
            });
        }

        [Fact]
        public async Task Handle_ValidData_ReturnsSuccess()
        {
            var file = CreateMockFile();
            var dtos = new List<TitleExcelDTO>
            {
                new TitleExcelDTO 
                { 
                    Name = "Valid Name", 
                    Description = "Desc", 
                    ColorHex = "#FFF", 
                    IconUrl = "url", 
                    RequirementType = "Level", 
                    RequirementQuantity = 5 
                }
            };
            
            _mockExcel.Setup(x => x.ExtractTitleDataAsync(file)).ReturnsAsync(dtos);
            _mockRepo.Setup(x => x.GetTitleByNameAsync("Valid Name", TitleStatus.Active)).ReturnsAsync((Tokki.Domain.Entities.Title?)null);
            _mockIdGen.Setup(x => x.GenerateCustom(10)).Returns("ID1");

            var result = await _handler.Handle(new ImportTitlesCommand { File = file }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be(1);
            result.Message.Should().Be("Nhập thành công 1 danh hiệu.");

            _mockRepo.Verify(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.Title>()), Times.Once);

            QACollector.LogTestCase("Title - Import", new TestCaseDetail
            {
                FunctionGroup     = "ImportTitlesCommandHandler",
                TestCaseID        = "TC-TTL-IMT-04",
                Description       = "Valid inputs imported successfully",
                ExpectedResult    = "Returns success counts",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid execution saves" }
            });
        }
    }
}
