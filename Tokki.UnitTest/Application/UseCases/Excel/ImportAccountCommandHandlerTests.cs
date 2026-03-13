using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.UseCases.Excel.Commands.ImportAccounts;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class ImportAccountCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ValidData_ShouldImportSuccessfully()
        {
            var mockRepo = MockAccountRepository.GetMock();
            var mockIdGen = MockIdGeneratorService.GetMock();
            var mockExcelService = new Mock<IExcelBaseService>();

            var excelResult = new ExcelImportResult<AccountExcelDTO>();
            excelResult.SuccessItems.Add(new ExcelSuccessDetail<AccountExcelDTO>
            {
                RowIndex = 2,
                Data = new AccountExcelDTO
                {
                    FullName = "Valid User",
                    Email = "ok@test.com",
                    Role = AccountRole.User,
                    Password = "123"
                }
            });

            mockExcelService.Setup(x => x.ImportAsync<AccountExcelDTO>(
                It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(excelResult);

            var handler = new ImportAccountCommandHandler(mockExcelService.Object, mockRepo.Object, mockIdGen.Object);
            var command = new ImportAccountCommand(new Mock<IFormFile>().Object);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.FailureList.Should().BeEmpty();
            result.Data.SuccessList.Should().HaveCount(1);
            result.Data.SuccessList[0].Email.Should().Be("ok@test.com");

            QACollector.LogTestCase("Excel - Account", new TestCaseDetail
            {
                FunctionGroup = "Import",
                TestCaseID = "TC-ACC-IMP-01",
                Description = "Import account with 100% valid data",
                ExpectedResult = "SuccessList has 1 item, FailureList is empty",
                StatusRound1 = "Passed",
                TestCaseType = "N", // Normal case
                TestDate = DateTime.Now.ToString("MM/dd/yyyy"),
                AppliedConditions = new List<string> {
                    "Valid .xlsx file",
                    "Server connection",
                    "New Email (Not exist)",
                    "Return Success"
                }
            });
        }

        [Fact]
        public async Task Handle_DuplicateEmailsInDB_ShouldReturnFailures()
        {
            var duplicateEmail = "exist@test.com";
            var mockRepo = MockAccountRepository.GetMock(existingEmails: new List<string> { duplicateEmail });
            var mockIdGen = MockIdGeneratorService.GetMock();
            var mockExcelService = new Mock<IExcelBaseService>();

            var excelResult = new ExcelImportResult<AccountExcelDTO>();
            excelResult.SuccessItems.Add(new ExcelSuccessDetail<AccountExcelDTO>
            {
                RowIndex = 2,
                Data = new AccountExcelDTO { FullName = "Exist User", Email = duplicateEmail, Role = AccountRole.User, Password = "123" }
            });

            mockExcelService.Setup(x => x.ImportAsync<AccountExcelDTO>(
                It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(excelResult);

            var handler = new ImportAccountCommandHandler(mockExcelService.Object, mockRepo.Object, mockIdGen.Object);

            var result = await handler.Handle(new ImportAccountCommand(new Mock<IFormFile>().Object), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.FailureList.Should().HaveCount(1);
            // Translate the expected failure reason to English to match the updated environment
            result.Data.FailureList.Should().ContainSingle()
                .Which.Reason.Should().Contain("đã tồn tại trên hệ thống");

            QACollector.LogTestCase("Excel - Account", new TestCaseDetail
            {
                FunctionGroup = "Import",
                TestCaseID = "TC-ACC-IMP-02",
                Description = "Import account with email already exists in DB",
                ExpectedResult = "Capture duplicate error into FailureList",
                StatusRound1 = "Passed",
                TestCaseType = "A", // Abnormal case (data conflict)
                TestDate = DateTime.Now.ToString("MM/dd/yyyy"),
                AppliedConditions = new List<string> {
                    "Valid .xlsx file",
                    "Server connection",
                    "Duplicate Email",
                    "Return Failure List"
                }
            });
        }
    }
}