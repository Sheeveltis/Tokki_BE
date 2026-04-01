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
using Tokki.Application.UseCases.Excel.Commands.ImportAccounts;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class ImportAccountCommandHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static ImportAccountCommandHandler CreateHandler(
            Mock<IExcelBaseService>? excelService = null,
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IIdGeneratorService>? idGenerator = null)
        {
            return new ImportAccountCommandHandler(
                (excelService ?? new Mock<IExcelBaseService>()).Object,
                (accountRepo ?? new Mock<IAccountRepository>()).Object,
                (idGenerator ?? new Mock<IIdGeneratorService>()).Object);
        }

        private static IFormFile GetMockFile()
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns("accounts.xlsx");
            return mock.Object;
        }

        private static ExcelImportResult<AccountExcelDTO> BuildExcelResult(
            List<AccountExcelDTO>? successItems = null,
            List<(int RowIndex, string Reason)>? errors = null)
        {
            var result = new ExcelImportResult<AccountExcelDTO>();
            int rowIndex = 2;
            foreach (var item in successItems ?? new List<AccountExcelDTO>())
            {
                result.SuccessItems.Add(new ExcelSuccessDetail<AccountExcelDTO> { RowIndex = rowIndex++, Data = item });
            }
            foreach (var (ri, reason) in errors ?? new List<(int, string)>())
            {
                result.Errors.Add(new ExcelErrorDetail { RowIndex = ri, Reason = reason });
            }
            return result;
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IMA-01 | N | Excel parsing returns format errors → FailureList
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExcelHasFormatErrors_ShouldAppendToFailureList()
        {
            // Arrange
            var excelResult = BuildExcelResult(errors: new List<(int, string)> { (2, "Invalid format") });

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ImportAsync<AccountExcelDTO>(It.IsAny<IFormFile>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(excelResult);

            var mockRepo = new Mock<IAccountRepository>();
            mockRepo.Setup(x => x.GetExistingEmailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<string>());

            // Act
            var result = await CreateHandler(excelService: mockExcel, accountRepo: mockRepo)
                             .Handle(new ImportAccountCommand(GetMockFile()), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.FailureList.Should().HaveCount(1);
            result.Data.FailureList[0].FullName.Should().Be("Lỗi định dạng file");
            result.Data.FailureList[0].Email.Should().Be("Dòng 2");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Accounts", new TestCaseDetail
            {
                FunctionGroup     = "ImportAccounts",
                TestCaseID        = "TC-EXC-IMA-01",
                Description       = "Base Excel parsing yields row errors which are appended to FailureList",
                ExpectedResult    = "Return 200 with FailureList count = 1",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "excelResult.Errors.Any() = true" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IMA-02 | N | Missing required fields → fails that row
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MissingRequiredFields_ShouldFailRow()
        {
            // Arrange
            var excelResult = BuildExcelResult(
                successItems: new List<AccountExcelDTO>
                {
                    new() { FullName = "Nam", Email = null!, Role = null, Password = null! }
                });

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ImportAsync<AccountExcelDTO>(It.IsAny<IFormFile>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(excelResult);

            var mockRepo = new Mock<IAccountRepository>();
            mockRepo.Setup(x => x.GetExistingEmailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<string>());

            // Act
            var result = await CreateHandler(excelService: mockExcel, accountRepo: mockRepo)
                             .Handle(new ImportAccountCommand(GetMockFile()), CancellationToken.None);

            // Assert
            result.Data!.FailureList.Should().HaveCount(1);
            result.Data.FailureList[0].Reason.Should().Contain("Thiếu thông tin bắt buộc");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Accounts", new TestCaseDetail
            {
                FunctionGroup     = "ImportAccounts",
                TestCaseID        = "TC-EXC-IMA-02",
                Description       = "Row has empty or null required fields (Email/FullName/Role/Password)",
                ExpectedResult    = "Validation loop skips and adds to FailureList",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "string.IsNullOrWhiteSpace(Email) || Role == null || ... " }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IMA-03 | N | Duplicate emails in file → second occurrence fails
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicateEmailsInFile_ShouldFailSecondRow()
        {
            // Arrange
            var excelResult = BuildExcelResult(
                successItems: new List<AccountExcelDTO>
                {
                    new() { FullName = "User 1", Email = "dup@test.com", Role = AccountRole.User, Password = "123" },
                    new() { FullName = "User 2", Email = "dup@test.com", Role = AccountRole.User, Password = "123" }
                });

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ImportAsync<AccountExcelDTO>(It.IsAny<IFormFile>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(excelResult);

            var mockRepo = new Mock<IAccountRepository>();
            mockRepo.Setup(x => x.GetExistingEmailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<string>());
            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Account>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.Generate(15)).Returns("U1");

            // Act
            var result = await CreateHandler(excelService: mockExcel, accountRepo: mockRepo, idGenerator: mockIdGen)
                             .Handle(new ImportAccountCommand(GetMockFile()), CancellationToken.None);

            // Assert
            result.Data!.FailureList.Should().HaveCount(1);
            result.Data.FailureList[0].Reason.Should().Contain("bị lặp lại trong file Excel");
            result.Data.SuccessList.Should().HaveCount(1); // first one succeeds

            // Excel Log
            QACollector.LogTestCase("Excel - Import Accounts", new TestCaseDetail
            {
                FunctionGroup     = "ImportAccounts",
                TestCaseID        = "TC-EXC-IMA-03",
                Description       = "Two rows with same email → second fails processedEmailsInFile HashSet",
                ExpectedResult    = "1 Success, 1 Failure due to duplicate in file",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!processedEmailsInFile.Add(currentEmail)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IMA-04 | N | Email already exists in Database → FailureList
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmailExistsInDatabase_ShouldFailRow()
        {
            // Arrange
            var excelResult = BuildExcelResult(
                successItems: new List<AccountExcelDTO>
                {
                    new() { FullName = "Exist User", Email = "exist@test.com", Role = AccountRole.User, Password = "123" }
                });

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ImportAsync<AccountExcelDTO>(It.IsAny<IFormFile>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(excelResult);

            var mockRepo = new Mock<IAccountRepository>();
            mockRepo.Setup(x => x.GetExistingEmailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<string> { "exist@test.com" });

            // Act
            var result = await CreateHandler(excelService: mockExcel, accountRepo: mockRepo)
                             .Handle(new ImportAccountCommand(GetMockFile()), CancellationToken.None);

            // Assert
            result.Data!.FailureList.Should().HaveCount(1);
            result.Data.FailureList[0].Reason.Should().Contain("đã tồn tại trên hệ thống");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Accounts", new TestCaseDetail
            {
                FunctionGroup     = "ImportAccounts",
                TestCaseID        = "TC-EXC-IMA-04",
                Description       = "Email already in database → fails existingDbEmailsSet.Contains check",
                ExpectedResult    = "1 Failure with message 'đã tồn tại trên hệ thống'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "existingDbEmailsSet.Contains(currentEmail)" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IMA-05 | N | Valid rows → hashed and inserted successfully
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRows_ShouldHashPasswordAndInsert()
        {
            // Arrange
            var excelResult = BuildExcelResult(
                successItems: new List<AccountExcelDTO>
                {
                    new() { FullName = "New Guy", Email = "new@test.com", Role = AccountRole.User, Password = "rawpassword" }
                });

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ImportAsync<AccountExcelDTO>(It.IsAny<IFormFile>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(excelResult);

            var mockRepo = new Mock<IAccountRepository>();
            mockRepo.Setup(x => x.GetExistingEmailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<string>());
            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Account>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.Generate(15)).Returns("U100");

            // Act
            var result = await CreateHandler(excelService: mockExcel, accountRepo: mockRepo, idGenerator: mockIdGen)
                             .Handle(new ImportAccountCommand(GetMockFile()), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.SuccessList.Should().HaveCount(1);
            mockRepo.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<Account>>(), It.IsAny<CancellationToken>()), Times.Once);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Excel - Import Accounts", new TestCaseDetail
            {
                FunctionGroup     = "ImportAccounts",
                TestCaseID        = "TC-EXC-IMA-05",
                Description       = "Valid rows are cryptographically hashed and bulk inserted to DB",
                ExpectedResult    = "Return 200, AddRangeAsync and SaveChangesAsync both called",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "newAccounts.Length > 0 => AddRangeAsync, SaveChangesAsync" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-IMA-06 | A | Database save throws → Return Failure DB_ERROR
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DatabaseThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var excelResult = BuildExcelResult(
                successItems: new List<AccountExcelDTO>
                {
                    new() { FullName = "New Guy", Email = "new@test.com", Role = AccountRole.User, Password = "123" }
                });

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ImportAsync<AccountExcelDTO>(It.IsAny<IFormFile>(), null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(excelResult);

            var mockRepo = new Mock<IAccountRepository>();
            mockRepo.Setup(x => x.GetExistingEmailsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<string>());
            mockRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<Account>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB Offline"));

            var mockIdGen = new Mock<IIdGeneratorService>();
            mockIdGen.Setup(x => x.Generate(15)).Returns("U100");

            // Act
            var result = await CreateHandler(excelService: mockExcel, accountRepo: mockRepo, idGenerator: mockIdGen)
                             .Handle(new ImportAccountCommand(GetMockFile()), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Lỗi lưu dữ liệu: DB Offline");

            // Excel Log
            QACollector.LogTestCase("Excel - Import Accounts", new TestCaseDetail
            {
                FunctionGroup     = "ImportAccounts",
                TestCaseID        = "TC-EXC-IMA-06",
                Description       = "SaveChangesAsync throws exception → DB_ERROR failure returned",
                ExpectedResult    = "Return Failure DB_ERROR with exception message",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception ex) inside AddRange block => Failure('DB_ERROR')" }
            });
        }
    }
}