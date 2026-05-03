using FluentValidation.TestHelper;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.QuestionBanks.Commands.ApproveQuestionBank;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands
{
    public class ApproveQuestionBanksCommandValidatorTests
    {
        private readonly ApproveQuestionBanksCommandValidator _validator;

        public ApproveQuestionBanksCommandValidatorTests()
        {
            _validator = new ApproveQuestionBanksCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_HasNoErrors()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> {"id1","id2" } };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     ="ApproveQuestionBanksCommandValidator",
                TestCaseID        ="ApproveQuestionBanksCommandValidator_01",
                Description       ="Valid",
                ExpectedResult    ="Passes",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Valid" }
            });
        }

        [Fact]
        public void Validate_NullIds_HasValidationError()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = null! };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu h?i là b?t bu?c.");
                  
            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     ="ApproveQuestionBanksCommandValidator",
                TestCaseID        ="ApproveQuestionBanksCommandValidator_02",
                Description       ="Null",
                ExpectedResult    ="Error",
                StatusRound1      ="Passed",
                TestCaseType      ="A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Null" }
            });
        }

        [Fact]
        public void Validate_EmptyIds_HasValidationError()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string>() };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds);

            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     ="ApproveQuestionBanksCommandValidator",
                TestCaseID        ="ApproveQuestionBanksCommandValidator_03",
                Description       ="Empty",
                ExpectedResult    ="Error",
                StatusRound1      ="Passed",
                TestCaseType      ="A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Empty" }
            });
        }

        [Fact]
        public void Validate_WhitespaceIds_HasValidationError()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> {"","" } };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu h?i không du?c r?ng.");

            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     ="ApproveQuestionBanksCommandValidator",
                TestCaseID        ="ApproveQuestionBanksCommandValidator_04",
                Description       ="Whitespace",
                ExpectedResult    ="Error",
                StatusRound1      ="Passed",
                TestCaseType      ="A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Whitespace" }
            });
        }

        [Fact]
        public void Validate_DuplicateTargetIds_HasValidationError()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> {"id1","id1" } };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu h?i b? trùng.");

            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     ="ApproveQuestionBanksCommandValidator",
                TestCaseID        ="ApproveQuestionBanksCommandValidator_05",
                Description       ="Duplicate",
                ExpectedResult    ="Error",
                StatusRound1      ="Passed",
                TestCaseType      ="A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Duplicates" }
            });
        }

        [Fact]
        public void Validate_DuplicateWhitespaceIds_HasValidationError()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> {"id1"," id1" } };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu h?i b? trùng.");

            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     ="ApproveQuestionBanksCommandValidator",
                TestCaseID        ="ApproveQuestionBanksCommandValidator_06",
                Description       ="Whitespace duplicates",
                ExpectedResult    ="Error",
                StatusRound1      ="Passed",
                TestCaseType      ="A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Whitespace" }
            });
        }
        [Fact]
        public void Validate_NullElementsMixedWithValid_HasNoErrors()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> {"id1", null!,"","id2" } };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     ="ApproveQuestionBanksCommandValidator",
                TestCaseID        ="ApproveQuestionBanksCommandValidator_07",
                Description       ="Filters",
                ExpectedResult    ="Pass",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Null" }
            });
        }
    }
}
