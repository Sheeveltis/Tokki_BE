using FluentValidation.TestHelper;
using Moq;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.QuestionBanks.Commands.RejectQuestionBank;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands
{
    public class RejectQuestionBanksCommandValidatorTests
    {
        private readonly RejectQuestionBanksCommandValidator _validator;

        public RejectQuestionBanksCommandValidatorTests()
        {
            _validator = new RejectQuestionBanksCommandValidator();
        }

        [Fact]
        public void Validate_QuestionBankIdsNull_ShouldHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = null, RejectReason ="Reason" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu h?i là b?t bu?c.");

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     ="RejectQuestionBanksCommandValidator",
                TestCaseID        ="RejectQuestionBanksCommandValidator_01",
                Description       ="QuestionBankIds is null",
                ExpectedResult    ="Error 'Danh sách mã câu h?i là b?t bu?c.'",
                StatusRound1      ="Passed",
                TestCaseType      ="A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"QuestionBankIds = null" }
            });
        }

        [Fact]
        public void Validate_QuestionBankIdsEmptyStringsOnly_ShouldHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> {"","" }, RejectReason ="Reason" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu h?i không du?c r?ng.");

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     ="RejectQuestionBanksCommandValidator",
                TestCaseID        ="RejectQuestionBanksCommandValidator_02",
                Description       ="QuestionBankIds contains only empty strings",
                ExpectedResult    ="Error 'Danh sách mã câu h?i không du?c r?ng.'",
                StatusRound1      ="Passed",
                TestCaseType      ="A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Empty strings in array" }
            });
        }

        [Fact]
        public void Validate_DuplicateIds_ShouldHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> {"id1","id1" }, RejectReason ="Reason" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu h?i b? trùng.");

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     ="RejectQuestionBanksCommandValidator",
                TestCaseID        ="RejectQuestionBanksCommandValidator_03",
                Description       ="QuestionBankIds has duplicates",
                ExpectedResult    ="Error about duplicate ids",
                StatusRound1      ="Passed",
                TestCaseType      ="A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Duplicate ids" }
            });
        }

        [Fact]
        public void Validate_EmptyRejectReason_ShouldHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> {"id1" }, RejectReason ="" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.RejectReason)
                  .WithErrorMessage("'Lý do t? ch?i' không du?c b? tr?ng.");

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     ="RejectQuestionBanksCommandValidator",
                TestCaseID        ="RejectQuestionBanksCommandValidator_04",
                Description       ="RejectReason is empty",
                ExpectedResult    ="Error 'Lý do t? ch?i' không du?c b? tr?ng.",
                StatusRound1      ="Passed",
                TestCaseType      ="A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"RejectReason empty" }
            });
        }

        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> {"id1","id2" }, RejectReason ="Not good" };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     ="RejectQuestionBanksCommandValidator",
                TestCaseID        ="RejectQuestionBanksCommandValidator_05",
                Description       ="Valid reject command",
                ExpectedResult    ="No errors",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Valid command" }
            });
        }

        [Fact]
        public void Validate_DuplicateIdsTrimmed_ShouldHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> {"id1"," id1" }, RejectReason ="Bad" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu h?i b? trùng.");

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     ="RejectQuestionBanksCommandValidator",
                TestCaseID        ="RejectQuestionBanksCommandValidator_06",
                Description       ="Duplicate ids distinguished only by whitespace",
                ExpectedResult    ="Error about duplicate ids",
                StatusRound1      ="Passed",
                TestCaseType      ="A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Whitespace duplicate ids" }
            });
        }
        [Fact]
        public void Validate_NullElementsMixedWithValid_ShouldNotHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> {"id1", null!,"","id2" }, RejectReason ="Reason" };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     ="RejectQuestionBanksCommandValidator",
                TestCaseID        ="RejectQuestionBanksCommandValidator_07",
                Description       ="Null",
                ExpectedResult    ="Validates",
                StatusRound1      ="Passed",
                TestCaseType      ="N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> {"Null" }
            });
        }
    }
}
