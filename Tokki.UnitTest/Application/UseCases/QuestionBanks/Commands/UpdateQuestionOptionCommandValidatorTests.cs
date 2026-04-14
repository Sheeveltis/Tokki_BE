using FluentValidation.TestHelper;
using Moq;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Update;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands
{
    public class UpdateQuestionOptionCommandValidatorTests
    {
        private readonly UpdateQuestionOptionCommandValidator _validator;

        public UpdateQuestionOptionCommandValidatorTests()
        {
            _validator = new UpdateQuestionOptionCommandValidator();
        }

        [Fact]
        public void Validate_AllEmpty_ShouldHaveError()
        {
            var command = new UpdateQuestionOptionCommand
            {
                KeyOption = null,
                Content = "",
                ImageUrl = "   ",
                IsCorrect = null
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x)
                  .WithErrorMessage("Không có dữ liệu cập nhật.");

            QACollector.LogTestCase("QuestionBank - Update Question Option", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionOptionCommandValidator",
                TestCaseID        = "TC-QB-UQO-01",
                Description       = "All fields empty or null should fail",
                ExpectedResult    = "Error 'Không có dữ liệu cập nhật.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Fields empty" }
            });
        }

        [Fact]
        public void Validate_InvalidKeyOption_ShouldHaveError()
        {
            var command = new UpdateQuestionOptionCommand
            {
                KeyOption = "5",
                Content = "A content"
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("KeyOption")
                  .WithErrorMessage("KeyOption phải là '1', '2', '3' hoặc '4'.");

            QACollector.LogTestCase("QuestionBank - Update Question Option", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionOptionCommandValidator",
                TestCaseID        = "TC-QB-UQO-02",
                Description       = "Invalid KeyOption outside 1-4",
                ExpectedResult    = "Error 'KeyOption phải là '1', '2', '3' hoặc '4'.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "KeyOption=5" }
            });
        }

        [Fact]
        public void Validate_ValidKeyOption_ShouldNotHaveError()
        {
            var command = new UpdateQuestionOptionCommand
            {
                KeyOption = "3"
            };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("QuestionBank - Update Question Option", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionOptionCommandValidator",
                TestCaseID        = "TC-QB-UQO-03",
                Description       = "Valid KeyOption only",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "KeyOption=3" }
            });
        }

        [Fact]
        public void Validate_OnlyContent_ShouldNotHaveError()
        {
            var command = new UpdateQuestionOptionCommand
            {
                Content = "Option Content"
            };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("QuestionBank - Update Question Option", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionOptionCommandValidator",
                TestCaseID        = "TC-QB-UQO-04",
                Description       = "Provide Content only",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Content provided" }
            });
        }

        [Fact]
        public void Validate_OnlyImageUrl_ShouldNotHaveError()
        {
            var command = new UpdateQuestionOptionCommand
            {
                ImageUrl = "http://image.url"
            };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("QuestionBank - Update Question Option", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionOptionCommandValidator",
                TestCaseID        = "TC-QB-UQO-05",
                Description       = "Provide ImageUrl only",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ImageUrl provided" }
            });
        }

        [Fact]
        public void Validate_OnlyIsCorrect_ShouldNotHaveError()
        {
            var command = new UpdateQuestionOptionCommand
            {
                IsCorrect = true
            };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("QuestionBank - Update Question Option", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionOptionCommandValidator",
                TestCaseID        = "TC-QB-UQO-06",
                Description       = "Provide IsCorrect only",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsCorrect provided" }
            });
        }
    }
}
