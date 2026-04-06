using FluentAssertions;
using FluentValidation.TestHelper;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts.Commands
{
    public class CreateAccountByAdminCommandValidatorTests
    {
        private readonly CreateAccountByAdminCommandValidator _validator;

        public CreateAccountByAdminCommandValidatorTests()
        {
            _validator = new CreateAccountByAdminCommandValidator();
        }

        private CreateAccountByAdminCommand GetValidCommand()
        {
            return new CreateAccountByAdminCommand
            {
                Email = "test@example.com",
                FullName = "Nguyễn Văn A",
                PhoneNumber = "0987654321",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
                Role = AccountRole.Staff
            };
        }

        // TC-ACC-CBA-01 | N | Happy path
        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveAnyErrors()
        {
            var command = GetValidCommand();
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandValidator",
                TestCaseID = "TC-ACC-CBA-01",
                Description = "Valid command should pass validation",
                ExpectedResult = "No validation errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid Email, FullName, PhoneNumber, DOB, Role" }
            });
        }

        // TC-ACC-CBA-02 | A | Missing Email
        [Fact]
        public void Validate_EmptyEmail_ShouldHaveError()
        {
            var command = GetValidCommand();
            command.Email = "";
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Email là bắt buộc.");

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandValidator",
                TestCaseID = "TC-ACC-CBA-02",
                Description = "Empty email fails validation",
                ExpectedResult = "Email là bắt buộc.",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty Email" }
            });
        }

        // TC-ACC-CBA-03 | A | Invalid Email Format
        [Fact]
        public void Validate_InvalidEmail_ShouldHaveError()
        {
            var command = GetValidCommand();
            command.Email = "invalid-email";
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Email không đúng định dạng.");

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandValidator",
                TestCaseID = "TC-ACC-CBA-03",
                Description = "Invalid email format fails validation",
                ExpectedResult = "Email không đúng định dạng.",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid Email" }
            });
        }

        // TC-ACC-CBA-04 | A | Exceed Max Length Email
        [Fact]
        public void Validate_EmailExceedsMaxLength_ShouldHaveError()
        {
            var command = GetValidCommand();
            command.Email = new string('a', 256) + "@example.com";
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Email không được vượt quá 255 ký tự.");

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandValidator",
                TestCaseID = "TC-ACC-CBA-04",
                Description = "Email exceeding max length fails validation",
                ExpectedResult = "Email không được vượt quá 255 ký tự.",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Email length > 255" }
            });
        }

        // TC-ACC-CBA-05 | A | Missing FullName
        [Fact]
        public void Validate_EmptyFullName_ShouldHaveError()
        {
            var command = GetValidCommand();
            command.FullName = "";
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.FullName)
                  .WithErrorMessage("Họ tên là bắt buộc.");

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandValidator",
                TestCaseID = "TC-ACC-CBA-05",
                Description = "Empty FullName fails validation",
                ExpectedResult = "Họ tên là bắt buộc.",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty FullName" }
            });
        }

        // TC-ACC-CBA-06 | A | Invalid PhoneNumber
        [Fact]
        public void Validate_InvalidPhoneNumber_ShouldHaveError()
        {
            var command = GetValidCommand();
            command.PhoneNumber = "1234567890"; // Invalid prefix
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
                  .WithErrorMessage("Số điện thoại không hợp lệ. Vui lòng nhập đúng số di động Việt Nam (10 chữ số, bắt đầu bằng 0).");

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandValidator",
                TestCaseID = "TC-ACC-CBA-06",
                Description = "Invalid PhoneNumber format fails validation",
                ExpectedResult = "Số điện thoại không hợp lệ...",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid Phone prefix" }
            });
        }

        // TC-ACC-CBA-07 | A | Invalid DateOfBirth (Future date)
        [Fact]
        public void Validate_FutureDateOfBirth_ShouldHaveError()
        {
            var command = GetValidCommand();
            command.DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.DateOfBirth)
                  .WithErrorMessage("Ngày sinh phải là ngày trong quá khứ.");

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandValidator",
                TestCaseID = "TC-ACC-CBA-07",
                Description = "Future DateOfBirth fails validation",
                ExpectedResult = "Ngày sinh phải là ngày trong quá khứ.",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DOB in future" }
            });
        }

        // TC-ACC-CBA-08 | A | Invalid Role
        [Fact]
        public void Validate_InvalidRole_ShouldHaveError()
        {
            var command = GetValidCommand();
            command.Role = AccountRole.Admin; // Cannot assign Admin
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Role)
                  .WithErrorMessage("Vai trò chỉ có thể là Staff hoặc User.");

            QACollector.LogTestCase("Account - Create By Admin", new TestCaseDetail
            {
                FunctionGroup = "CreateAccountByAdminCommandValidator",
                TestCaseID = "TC-ACC-CBA-08",
                Description = "Invalid Role (e.g. Admin) fails validation",
                ExpectedResult = "Vai trò chỉ có thể là Staff hoặc User.",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Role = Admin" }
            });
        }
    }
}
