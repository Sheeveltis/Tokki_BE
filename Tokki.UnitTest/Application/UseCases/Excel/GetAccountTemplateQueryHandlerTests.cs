using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Application.UseCases.Excel.Queries.GetTemplate;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class GetAccountTemplateQueryHandlerTests
    {
        private readonly Mock<IExcelBaseService> _mockExcelService;
        private readonly GetAccountTemplateQueryHandler _handler;

        public GetAccountTemplateQueryHandlerTests()
        {
            _mockExcelService = new Mock<IExcelBaseService>();
            _handler = new GetAccountTemplateQueryHandler(_mockExcelService.Object);
        }

        [Fact]
        public async Task Handle_Success_ShouldReturnFileBytes()
        {
            _mockExcelService.Setup(x => x.GenerateTemplateAsync<AccountExcelDTO>(It.IsAny<string>()))
                             .ReturnsAsync(new byte[] { 0x1, 0x2, 0x3 });

            var result = await _handler.Handle(new GetAccountTemplateQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.FileBytes.Should().NotBeEmpty();
            result.Data.FileName.Should().Contain("Template_Import_Account");

            QACollector.LogTestCase("Excel - Account", new TestCaseDetail
            {
                FunctionGroup = "Template",
                TestCaseID = "TC-ACC-TPL-01",
                Description = "Download import account template successfully",
                ExpectedResult = "Return byte array and .xlsx filename",
                StatusRound1 = "Passed",
                TestCaseType = "N", // Normal case
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {
                    "Server is running",
                    "Return Excel File"
                }
            });
        }

        [Fact]
        public async Task Handle_Exception_ShouldReturnFailure()
        {
            _mockExcelService.Setup(x => x.GenerateTemplateAsync<AccountExcelDTO>(It.IsAny<string>()))
                             .ThrowsAsync(new Exception("Server error creating template"));

            var result = await _handler.Handle(new GetAccountTemplateQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().NotBe(200);

            QACollector.LogTestCase("Excel - Account", new TestCaseDetail
            {
                FunctionGroup = "Template",
                TestCaseID = "TC-ACC-TPL-02",
                Description = "System error while downloading template",
                ExpectedResult = "Catch Exception and return Failure Result",
                StatusRound1 = "Passed",
                TestCaseType = "A", // Abnormal case (Exception)
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {
                    "Server error simulation",
                    "Exception propagated"
                }
            });
        }
    }
}