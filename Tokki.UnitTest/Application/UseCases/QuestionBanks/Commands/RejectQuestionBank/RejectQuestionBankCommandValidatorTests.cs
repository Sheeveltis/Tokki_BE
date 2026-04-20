using FluentAssertions;
using FluentValidation.TestHelper;
using System.Collections.Generic;
using Tokki.Application.UseCases.QuestionBanks.Commands.RejectQuestionBank;
using Tokki.UnitTest.Utilities;
using Xunit;
using System;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands.RejectQuestionBank
{
    public class RejectQuestionBankCommandValidatorTests
    {
        private readonly RejectQuestionBanksCommandValidator _validator = new();

        // ═══════════════════════════════════════════════════════════
        // RejectQuestionBanksCommandValidator_01 | A | Null QuestionBankIds -> Fails
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void Validate_NullQuestionBankIds_ShouldHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = null, RejectReason = "A" };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds).WithErrorMessage("Danh sách mã câu hỏi là bắt buộc.");
            
            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup = "RejectQuestionBanksCommandValidator",
                TestCaseID = "RejectQuestionBanksCommandValidator_01",
                Description = "Validator returns error if QuestionBankIds is null",
                ExpectedResult = "Validation failed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionBankIds is null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // RejectQuestionBanksCommandValidator_02 | A | Empty QuestionBankIds -> Fails
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void Validate_EmptyItemsQuestionBankIds_ShouldHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { " ", "" }, RejectReason = "A" };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds).WithErrorMessage("Danh sách mã câu hỏi không được rỗng.");

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup = "RejectQuestionBanksCommandValidator",
                TestCaseID = "RejectQuestionBanksCommandValidator_02",
                Description = "Validator returns error if QuestionBankIds contains only empty logic",
                ExpectedResult = "Validation failed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "empty items in list" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // RejectQuestionBanksCommandValidator_03 | A | Duplicate QuestionBankIds -> Fails
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void Validate_DuplicateQuestionBankIds_ShouldHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "qb-1", "qb-1" }, RejectReason = "A" };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds).WithErrorMessage("Danh sách mã câu hỏi bị trùng.");

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup = "RejectQuestionBanksCommandValidator",
                TestCaseID = "RejectQuestionBanksCommandValidator_03",
                Description = "Validator returns error if QuestionBankIds contains duplicates",
                ExpectedResult = "Validation failed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "duplicate list items" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // RejectQuestionBanksCommandValidator_04 | A | Empty RejectReason -> Fails
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void Validate_EmptyRejectReason_ShouldHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "qb-1" }, RejectReason = "" };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.RejectReason);

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup = "RejectQuestionBanksCommandValidator",
                TestCaseID = "RejectQuestionBanksCommandValidator_04",
                Description = "Validator returns error if RejectReason is empty",
                ExpectedResult = "Validation failed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "RejectReason empty" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // RejectQuestionBanksCommandValidator_05 | N | Valid -> Success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public void Validate_Valid_ShouldNotHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "qb-1", "qb-2" }, RejectReason = "Not good" };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Question Bank - Reject", new TestCaseDetail
            {
                FunctionGroup = "RejectQuestionBanksCommandValidator",
                TestCaseID = "RejectQuestionBanksCommandValidator_05",
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
