using FluentAssertions;
using FluentValidation.TestHelper;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Create;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands.QuestionOptions
{
    public class CreateQuestionOptionCommandValidatorTests
    {
        private readonly CreateQuestionOptionCommandValidator _validator;

        public CreateQuestionOptionCommandValidatorTests()
        {
            _validator = new CreateQuestionOptionCommandValidator();
        }

        // TC-QB-COV-01 | A | KeyOption Empty -> Error
        [Fact]
        public void Validate_EmptyKeyOption_ShouldHaveError()
        {
            var command = new CreateQuestionOptionCommand { KeyOption = "" };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.KeyOption);

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionOptionCommandValidator",
                TestCaseID = "TC-QB-COV-01",
                Description = "Empty KeyOption triggers immediate requirement constraint",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "KeyOption is empty string" }
            });
        }

        // TC-QB-COV-02 | A | KeyOption out of bounds -> Error
        [Fact]
        public void Validate_InvalidKeyOptionBounds_ShouldHaveError()
        {
            var command = new CreateQuestionOptionCommand { KeyOption = "5" };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.KeyOption)
                  .WithErrorMessage("KeyOption phải là '1', '2', '3' hoặc '4'.");

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionOptionCommandValidator",
                TestCaseID = "TC-QB-COV-02",
                Description = "Restricts multiple choice answers to exactly 4 classical slots",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "KeyOption equals 5" }
            });
        }

        // TC-QB-COV-03 | A | Both Content and ImageUrl Empty -> Error
        [Fact]
        public void Validate_BothMediaEmpty_ShouldHaveError()
        {
            var command = new CreateQuestionOptionCommand { KeyOption = "1", Content = "", ImageUrl = "" };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Content)
                  .WithErrorMessage("Đáp án phải có nội dung text hoặc ảnh.");

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionOptionCommandValidator",
                TestCaseID = "TC-QB-COV-03",
                Description = "Evaluates inter-dependency forcing an explicit data medium to appear on front-end",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Both Text and Image are empty" }
            });
        }

        // TC-QB-COV-04 | N | Content Only -> Pass
        [Fact]
        public void Validate_ContentOnly_ShouldNotHaveError()
        {
            var command = new CreateQuestionOptionCommand { KeyOption = "2", Content = "Dap An" };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionOptionCommandValidator",
                TestCaseID = "TC-QB-COV-04",
                Description = "Satisfies validation requiring at least primary text format medium",
                ExpectedResult = "No Errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Content provided" }
            });
        }

        // TC-QB-COV-05 | N | ImageUrl Only -> Pass
        [Fact]
        public void Validate_ImageOnly_ShouldNotHaveError()
        {
            var command = new CreateQuestionOptionCommand { KeyOption = "3", ImageUrl = "https://img.jpg" };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionOptionCommandValidator",
                TestCaseID = "TC-QB-COV-05",
                Description = "Satisfies validation requiring at least alternate photo medium format",
                ExpectedResult = "No Errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ImageUrl provided" }
            });
        }

        // TC-QB-COV-06 | N | Both Contents -> Pass
        [Fact]
        public void Validate_BothFormats_ShouldNotHaveError()
        {
            var command = new CreateQuestionOptionCommand 
            { 
                KeyOption = "4", 
                ImageUrl = "https://img.jpg",
                Content = "See Image Below"
            };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionOptionCommandValidator",
                TestCaseID = "TC-QB-COV-06",
                Description = "Combination media gracefully accepted",
                ExpectedResult = "No Errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Combined Media and Text" }
            });
        }
    }
}
