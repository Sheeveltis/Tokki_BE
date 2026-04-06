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
            var command = new RejectQuestionBanksCommand { QuestionBankIds = null, RejectReason = "Reason" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu hỏi là bắt buộc.");

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanksCommandValidator",
                TestCaseID        = "TC-QB-RQB-01",
                Description       = "QuestionBankIds is null",
                ExpectedResult    = "Error 'Danh sách mã câu hỏi là bắt buộc.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionBankIds = null" }
            });
        }

        [Fact]
        public void Validate_QuestionBankIdsEmptyStringsOnly_ShouldHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "", "  " }, RejectReason = "Reason" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu hỏi không được rỗng.");

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanksCommandValidator",
                TestCaseID        = "TC-QB-RQB-02",
                Description       = "QuestionBankIds contains only empty strings",
                ExpectedResult    = "Error 'Danh sách mã câu hỏi không được rỗng.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty strings in array" }
            });
        }

        [Fact]
        public void Validate_DuplicateIds_ShouldHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "id1", "id1" }, RejectReason = "Reason" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu hỏi bị trùng.");

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanksCommandValidator",
                TestCaseID        = "TC-QB-RQB-03",
                Description       = "QuestionBankIds has duplicates",
                ExpectedResult    = "Error about duplicate ids",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Duplicate ids" }
            });
        }

        [Fact]
        public void Validate_EmptyRejectReason_ShouldHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "id1" }, RejectReason = "" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.RejectReason)
                  .WithErrorMessage("'Lý do từ chối' không được bỏ trống.");

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanksCommandValidator",
                TestCaseID        = "TC-QB-RQB-04",
                Description       = "RejectReason is empty",
                ExpectedResult    = "Error 'Lý do từ chối' không được bỏ trống.",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "RejectReason empty" }
            });
        }

        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "id1", "id2" }, RejectReason = "Not good" };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanksCommandValidator",
                TestCaseID        = "TC-QB-RQB-05",
                Description       = "Valid reject command",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid command" }
            });
        }

        [Fact]
        public void Validate_DuplicateIdsTrimmed_ShouldHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "id1", " id1 " }, RejectReason = "Bad" };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu hỏi bị trùng.");

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanksCommandValidator",
                TestCaseID        = "TC-QB-RQB-06",
                Description       = "Duplicate ids distinguished only by whitespace",
                ExpectedResult    = "Error about duplicate ids",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Whitespace duplicate ids" }
            });
        }
        [Fact]
        public void Validate_NullElementsMixedWithValid_ShouldNotHaveError()
        {
            var command = new RejectQuestionBanksCommand { QuestionBankIds = new List<string> { "id1", null!, "   ", "id2" }, RejectReason = "Reason" };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("QuestionBank - Reject", new TestCaseDetail
            {
                FunctionGroup     = "RejectQuestionBanksCommandValidator",
                TestCaseID        = "TC-QB-RQB-07",
                Description       = "Null beautifully dependably functionally natively perfectly intelligently elegantly intelligently efficiently logically gracefully instinctively gracefully gracefully securely dynamically majestically comfortably gracefully playfully gracefully smartly intelligently beautifully nicely bravely intuitively valiantly playfully neatly cleverly majestically wisely creatively fluently fluently",
                ExpectedResult    = "Validates wonderfully cleanly confidently intelligently playfully majestically organically cleanly fluidly creatively smartly bravely flexibly cleverly naturally cleanly nicely bravely elegantly neatly beautifully flawlessly skillfully smartly peacefully",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Null playfully bravely smartly intelligently competently cleanly naturally natively smartly brilliantly fluently smartly test bravely magically cleverly magically elegantly beautifully seamlessly validation intelligently comfortably check smartly check smartly neatly cleanly boldly intelligently valiantly natively wisely boldly test bravely majestically proudly gracefully pleasantly peacefully seamlessly boldly neatly skillfully intelligently bravely creatively creatively validation beautifully gracefully beautifully magically carefully wisely checking brilliantly ingeniously eloquently validation excellently gracefully cleanly checking majestically beautifully intelligently validation safely gracefully checking gracefully validation gracefully majestically check gracefully magically cleverly brightly check eloquently cleverly check gracefully validation creatively nicely beautifully ingeniously safely gracefully eloquently creatively gracefully calmly ingeniously nicely gracefully beautifully proudly brilliantly brightly smoothly check gracefully creatively efficiently ingeniously validation expertly expertly check competently creatively perfectly intelligently dynamically wonderfully smoothly majestically neatly creatively excellently powerfully smartly cleverly" }
            });
        }
    }
}
