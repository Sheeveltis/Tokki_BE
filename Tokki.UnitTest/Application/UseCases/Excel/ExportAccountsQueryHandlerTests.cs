using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Application.UseCases.Excel.Queries.ExportAccounts;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class ExportAccountsQueryHandlerTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory
        // ─────────────────────────────────────────────────────────────────────
        private static ExportAccountsQueryHandler CreateHandler(
            Mock<IExcelBaseService>? excelService = null,
            Mock<IAccountRepository>? accountRepo = null)
        {
            return new ExportAccountsQueryHandler(
                (excelService ?? new Mock<IExcelBaseService>()).Object,
                (accountRepo ?? new Mock<IAccountRepository>()).Object);
        }

        // ExportAsync has 3 parameters: data, sheetName, ignoredColumns (NO CancellationToken)
        private static void SetupExportAsync(Mock<IExcelBaseService> mock, byte[]? returnBytes = null)
        {
            mock.Setup(x => x.ExportAsync(
                    It.IsAny<IEnumerable<AccountExcelDTO>>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>?>()))
                .ReturnsAsync(returnBytes ?? new byte[] { 0x01 });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-EXA-01 | A | Repository returns null → DATA_NULL failure
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AccountsNull_ShouldReturnDataNullFailure()
        {
            // Arrange
            var mockRepo = new Mock<IAccountRepository>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((List<Account>?)null);

            // Act
            var result = await CreateHandler(accountRepo: mockRepo).Handle(new ExportAccountsQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Danh sách tài khoản trống");

            // Excel Log
            QACollector.LogTestCase("Excel - Export Accounts", new TestCaseDetail
            {
                FunctionGroup     = "ExportAccounts",
                TestCaseID        = "TC-EXC-EXA-01",
                Description       = "Repository returns null collection",
                ExpectedResult    = "Return Failure DATA_NULL",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "accounts == null" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-EXA-02 | A | Repository returns empty list → export empty file
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AccountsEmpty_ShouldExportEmptyFile()
        {
            // Arrange
            var mockRepo = new Mock<IAccountRepository>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Account>());

            var mockExcel = new Mock<IExcelBaseService>();
            SetupExportAsync(mockExcel);

            // Act
            var result = await CreateHandler(excelService: mockExcel, accountRepo: mockRepo)
                             .Handle(new ExportAccountsQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.FileBytes.Should().NotBeNull();
            result.Data.FileName.Should().Contain("Accounts_Export_");

            // Excel Log
            QACollector.LogTestCase("Excel - Export Accounts", new TestCaseDetail
            {
                FunctionGroup     = "ExportAccounts",
                TestCaseID        = "TC-EXC-EXA-02",
                Description       = "Repository returns empty collection, proceeds to generate file with headers only",
                ExpectedResult    = "Return 200 with FileBytes",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "accounts.Count == 0" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-EXA-03 | N | Valid data → properly mapped and file exported
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidData_ShouldReturnWrappedFileBytes()
        {
            // Arrange
            var accounts = new List<Account>
            {
                new() { FullName = "Valid User", Email = "valid@test.com", Role = AccountRole.User, DateOfBirth = new DateTime(2000, 1, 1), PhoneNumber = "123" }
            };
            var mockRepo = new Mock<IAccountRepository>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(accounts);

            var mockExcel = new Mock<IExcelBaseService>();
            SetupExportAsync(mockExcel, new byte[] { 0x01, 0x02 });

            // Act
            var result = await CreateHandler(excelService: mockExcel, accountRepo: mockRepo)
                             .Handle(new ExportAccountsQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.FileName.Should().Contain("Accounts_Export_");
            result.Data.FileBytes.Should().NotBeEmpty();

            // Excel Log
            QACollector.LogTestCase("Excel - Export Accounts", new TestCaseDetail
            {
                FunctionGroup     = "ExportAccounts",
                TestCaseID        = "TC-EXC-EXA-03",
                Description       = "Valid data extraction translates to generated file bytes correctly",
                ExpectedResult    = "Return 200 with FileBytes and Filename",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "accounts mapped to AccountExcelDTO then ExportAsync" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-EXA-04 | N | Null properties in data → ExportAsync still called
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullPropertiesInData_ShouldFallbackSafely()
        {
            // Arrange
            var accounts = new List<Account>
            {
                new() { FullName = null, Email = null, PhoneNumber = null, Role = AccountRole.User }
            };
            var mockRepo = new Mock<IAccountRepository>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(accounts);

            var mockExcel = new Mock<IExcelBaseService>();
            SetupExportAsync(mockExcel);

            // Act
            var result = await CreateHandler(excelService: mockExcel, accountRepo: mockRepo)
                             .Handle(new ExportAccountsQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockExcel.Verify(x => x.ExportAsync(
                    It.IsAny<IEnumerable<AccountExcelDTO>>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>?>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Excel - Export Accounts", new TestCaseDetail
            {
                FunctionGroup     = "ExportAccounts",
                TestCaseID        = "TC-EXC-EXA-04",
                Description       = "Fields with null values still allow ExportAsync to be called",
                ExpectedResult    = "Return 200, ExportAsync called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "a.FullName ?? 'N/A'", "a.Email ?? 'N/A'" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-EXA-05 | N | Password column is included in ignoredColumns
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_IgnoredColumnsSetup_ShouldExcludePassword()
        {
            // Arrange
            var mockRepo = new Mock<IAccountRepository>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Account>());

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ExportAsync(
                    It.IsAny<IEnumerable<AccountExcelDTO>>(),
                    It.IsAny<string>(),
                    It.Is<List<string>?>(l => l != null && l.Contains("Password"))))
                     .ReturnsAsync(new byte[] { 0x01 });

            // Act
            var result = await CreateHandler(excelService: mockExcel, accountRepo: mockRepo)
                             .Handle(new ExportAccountsQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockExcel.Verify(x => x.ExportAsync(
                    It.IsAny<IEnumerable<AccountExcelDTO>>(),
                    It.IsAny<string>(),
                    It.Is<List<string>?>(l => l != null && l.Contains("Password"))), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Excel - Export Accounts", new TestCaseDetail
            {
                FunctionGroup     = "ExportAccounts",
                TestCaseID        = "TC-EXC-EXA-05",
                Description       = "Password column is in ignored columns configuration list",
                ExpectedResult    = "Return 200, ignoredList contains Password",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ignoredColumns: new List<string> { nameof(AccountExcelDTO.Password) }" }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // TC-EXC-EXA-06 | A | IExcelBaseService throws → return EXPORT_EXCEPTION
        // ═══════════════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExcelExportThrows_ShouldReturnExceptionMessage()
        {
            // Arrange
            var mockRepo = new Mock<IAccountRepository>();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Account>());

            var mockExcel = new Mock<IExcelBaseService>();
            mockExcel.Setup(x => x.ExportAsync(
                    It.IsAny<IEnumerable<AccountExcelDTO>>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>?>()))
                     .ThrowsAsync(new Exception("Write protection"));

            // Act
            var result = await CreateHandler(excelService: mockExcel, accountRepo: mockRepo)
                             .Handle(new ExportAccountsQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Write protection");

            // Excel Log
            QACollector.LogTestCase("Excel - Export Accounts", new TestCaseDetail
            {
                FunctionGroup     = "ExportAccounts",
                TestCaseID        = "TC-EXC-EXA-06",
                Description       = "General exception occurs during core Excel byte manipulation",
                ExpectedResult    = "Return Failure EXPORT_EXCEPTION",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch(Exception ex) => Failure EXPORT_EXCEPTION" }
            });
        }
    }
}