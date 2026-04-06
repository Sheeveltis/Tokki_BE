using FluentValidation.TestHelper;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Tokki.Application.UseCases.UserExam.Commands.SyncMCQProgress;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.UserExam.Commands
{
    public class SyncMCQProgressCommandValidatorTests
    {
        private readonly SyncMCQProgressCommandValidator _validator;

        public SyncMCQProgressCommandValidatorTests()
        {
            _validator = new SyncMCQProgressCommandValidator();
        }

        [Fact]
        public void Validate_EmptyAnswers_ShouldHaveError()
        {
            var command = new SyncMCQProgressCommand { Answers = new List<MCQAnswerDto>() };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Answers)
                  .WithErrorMessage("Danh sách đáp án không được để trống.");

            QACollector.LogTestCase("UserExam - Sync MCQ Progress Validator", new TestCaseDetail
            {
                FunctionGroup     = "SyncMCQProgressCommandValidator",
                TestCaseID        = "TC-UE-SMPV-01",
                Description       = "Empty Answers list",
                ExpectedResult    = "Error 'Danh sách đáp án không được để trống.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Answers empty list" }
            });
        }

        [Fact]
        public void Validate_NullAnswers_ShouldHaveError()
        {
            var command = new SyncMCQProgressCommand { Answers = null };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Answers)
                  .WithErrorMessage("Danh sách đáp án không được để trống.");

            QACollector.LogTestCase("UserExam - Sync MCQ Progress Validator", new TestCaseDetail
            {
                FunctionGroup     = "SyncMCQProgressCommandValidator",
                TestCaseID        = "TC-UE-SMPV-02",
                Description       = "Answers is null",
                ExpectedResult    = "Error 'Danh sách đáp án không được để trống.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Answers null" }
            });
        }

        [Fact]
        public void Validate_AnswersExceed100_ShouldHaveError()
        {
            var command = new SyncMCQProgressCommand
            {
                Answers = Enumerable.Range(1, 101).Select(i => new MCQAnswerDto { UserQuestionId = "q1", SelectedOptionId = "o1" }).ToList()
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Answers)
                  .WithErrorMessage("Mỗi lần đồng bộ không quá 100 câu để đảm bảo hiệu năng.");

            QACollector.LogTestCase("UserExam - Sync MCQ Progress Validator", new TestCaseDetail
            {
                FunctionGroup     = "SyncMCQProgressCommandValidator",
                TestCaseID        = "TC-UE-SMPV-03",
                Description       = "Answers count > 100",
                ExpectedResult    = "Error max count 100 limit breached",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Answers count=101" }
            });
        }

        [Fact]
        public void Validate_EmptyUserQuestionId_ShouldHaveError()
        {
            var command = new SyncMCQProgressCommand
            {
                Answers = new List<MCQAnswerDto> { new MCQAnswerDto { UserQuestionId = "", SelectedOptionId = "o1" } }
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("Answers[0].UserQuestionId")
                  .WithErrorMessage("ID câu hỏi người dùng không được để trống.");

            QACollector.LogTestCase("UserExam - Sync MCQ Progress Validator", new TestCaseDetail
            {
                FunctionGroup     = "SyncMCQProgressCommandValidator",
                TestCaseID        = "TC-UE-SMPV-04",
                Description       = "Item with empty UserQuestionId",
                ExpectedResult    = "Error 'ID câu hỏi người dùng không được để trống.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty UserQuestionId in children limits rules" }
            });
        }

        [Fact]
        public void Validate_WhitespaceSelectedOptionId_ShouldHaveError()
        {
            var command = new SyncMCQProgressCommand
            {
                Answers = new List<MCQAnswerDto> { new MCQAnswerDto { UserQuestionId = "q1", SelectedOptionId = "   " } }
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("Answers[0].SelectedOptionId")
                  .WithErrorMessage("ID lựa chọn không hợp lệ.");

            QACollector.LogTestCase("UserExam - Sync MCQ Progress Validator", new TestCaseDetail
            {
                FunctionGroup     = "SyncMCQProgressCommandValidator",
                TestCaseID        = "TC-UE-SMPV-05",
                Description       = "SelectedOptionId with only whitespaces",
                ExpectedResult    = "Error 'ID lựa chọn không hợp lệ.'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SelectedOptionId mapped whitespace" }
            });
        }

        [Fact]
        public void Validate_ValidCommandWithNullOption_ShouldNotHaveError()
        {
            var command = new SyncMCQProgressCommand
            {
                Answers = new List<MCQAnswerDto> { new MCQAnswerDto { UserQuestionId = "q1", SelectedOptionId = null } }
            };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("UserExam - Sync MCQ Progress Validator", new TestCaseDetail
            {
                FunctionGroup     = "SyncMCQProgressCommandValidator",
                TestCaseID        = "TC-UE-SMPV-06",
                Description       = "Null SelectedOptionId is valid for skipping/clearing",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SelectedOptionId=null" }
            });
        }
    }
}
