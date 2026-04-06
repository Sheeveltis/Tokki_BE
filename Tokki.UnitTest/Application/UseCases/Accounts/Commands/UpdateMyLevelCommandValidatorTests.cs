using FluentAssertions;
using FluentValidation.TestHelper;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.Accounts.Commands.UpdateMyLevel;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Accounts.Commands
{
    public class UpdateMyLevelCommandValidatorTests
    {
        private readonly UpdateMyLevelCommandValidator _validator;

        public UpdateMyLevelCommandValidatorTests()
        {
            _validator = new UpdateMyLevelCommandValidator();
        }

        // TC-ACC-UMLV-01 | N | Level is Null
        [Fact]
        public void Validate_NullLevel_ShouldNotHaveError()
        {
            var command = new UpdateMyLevelCommand { UserId = "u1", Level = null };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Account - Update My Level Valid", new TestCaseDetail
            {
                FunctionGroup = "UpdateMyLevelCommandValidator",
                TestCaseID = "TC-ACC-UMLV-01",
                Description = "Null level passes validation seamlessly",
                ExpectedResult = "No errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Level = null" }
            });
        }

        // TC-ACC-UMLV-02 | N | Level is valid enum int
        [Fact]
        public void Validate_ValidEnumLevel_ShouldNotHaveError()
        {
            var command = new UpdateMyLevelCommand { UserId = "u1", Level = Tokki.Domain.Enums.TopicLevel.Level1 }; // TopicLevel Enum typically 1,2,3...
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Account - Update My Level Valid", new TestCaseDetail
            {
                FunctionGroup = "UpdateMyLevelCommandValidator",
                TestCaseID = "TC-ACC-UMLV-02",
                Description = "Valid TopicLevel enum value passes",
                ExpectedResult = "No errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Level = valid int value for TopicLevel" }
            });
        }

        // TC-ACC-UMLV-03 | A | Invalid enum level
        [Fact]
        public void Validate_InvalidEnumLevel_ShouldHaveError()
        {
            var command = new UpdateMyLevelCommand { UserId = "u1", Level = (Tokki.Domain.Enums.TopicLevel)999 };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Level)
                  .WithErrorMessage("*không hợp lệ*"); // Using wildcard pattern match equivalent via Contain manually or just check it threw

            QACollector.LogTestCase("Account - Update My Level Valid", new TestCaseDetail
            {
                FunctionGroup = "UpdateMyLevelCommandValidator",
                TestCaseID = "TC-ACC-UMLV-03",
                Description = "Out-of-bound level value fails with specific error message formatting",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Level = 999" }
            });
        }
    }
}
