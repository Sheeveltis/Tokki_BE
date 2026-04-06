using FluentAssertions;
using FluentValidation.TestHelper;
using System.Collections.Generic;
using Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank;
using Tokki.UnitTest.Utilities;
using Xunit;
using System;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank
{
    public class UpdateQuestionBankCommandValidatorTests
    {
        private readonly UpdateQuestionBankCommandValidator _validator = new();

        // ═══════════════════════════════════════════════════════════
        // TC-QB-V-UP-01 | A | Empty QuestionBankId -> Fails
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void Validate_EmptyQuestionBankId_ShouldHaveError()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId = "" };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.QuestionBankId);
            
            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionBankCommandValidator",
                TestCaseID = "TC-QB-V-UP-01",
                Description = "Validator returns error if QuestionBankId is empty",
                ExpectedResult = "Validation failed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionBankId is empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-V-UP-02 | N | Valid -> Success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void Validate_Valid_ShouldNotHaveError()
        {
            var command = new UpdateQuestionBankCommand { QuestionBankId = "qb-1" };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionBankCommandValidator",
                TestCaseID = "TC-QB-V-UP-02",
                Description = "Validator passes on valid command",
                ExpectedResult = "No errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid command" }
            });
        }
    }
}
