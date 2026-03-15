using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Application.UseCases.Excel.Queries.ExportAccounts;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class ExportAccountsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WithData_ShouldExportSuccessfully()
        {
            var sampleData = MockAccountRepository.GetSampleAccountsForExport();
            var mockRepo = MockAccountRepository.GetMock(existingAccounts: sampleData);
            var mockExcelService = new Mock<IExcelBaseService>();

            mockExcelService.Setup(x => x.ExportAsync(
                It.IsAny<IEnumerable<AccountExcelDTO>>(),
                It.IsAny<string>(),
                It.IsAny<List<string>>()))
                .ReturnsAsync(new byte[] { 0x99, 0x88 });

            var handler = new ExportAccountsQueryHandler(mockExcelService.Object, mockRepo.Object);

            var result = await handler.Handle(new ExportAccountsQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.FileBytes.Should().NotBeNull();
            result.Data.FileName.Should().Contain("Accounts_Export_");

            QACollector.LogTestCase("Excel - Account", new TestCaseDetail
            {
                FunctionGroup = "Export",
                TestCaseID = "TC-ACC-EXP-01",
                Description = "Export accounts list successfully with data",
                ExpectedResult = "Return Excel file containing data",
                StatusRound1 = "Passed",
                TestCaseType = "N", // Normal case
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {
                    "DB has account data",
                    "Return Excel File"
                }
            });
        }

        [Fact]
        public async Task Handle_NullData_ShouldReturnFailure()
        {
            // Arrange — setup GetAllAsync trả về null để trigger check accounts == null
            var mockRepo = new Mock<IAccountRepository>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((List<Tokki.Domain.Entities.Account>?)null);

            var mockExcelService = new Mock<IExcelBaseService>();

            var handler = new ExportAccountsQueryHandler(
                mockExcelService.Object,
                mockRepo.Object);

            // Act
            var result = await handler.Handle(new ExportAccountsQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().NotBe(200);

            QACollector.LogTestCase("Excel - Account", new TestCaseDetail
            {
                FunctionGroup = "Export",
                TestCaseID = "TC-ACC-EXP-02",
                Description = "Attempt to export when account list is null",
                ExpectedResult = "Return failure result and do not export file",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
        {
            "GetAllAsync returns null",
            "Return Failure with DATA_NULL error"
        }
            });
        }
    }
}