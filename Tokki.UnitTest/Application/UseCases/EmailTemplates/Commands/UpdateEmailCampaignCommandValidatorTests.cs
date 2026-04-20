using FluentAssertions;
using FluentValidation.TestHelper;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailCampaign;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates.Commands
{
    public class UpdateEmailCampaignCommandValidatorTests
    {
        private readonly UpdateEmailCampaignCommandValidator _validator;

        public UpdateEmailCampaignCommandValidatorTests()
        {
            _validator = new UpdateEmailCampaignCommandValidator();
        }

        // UpdateEmailCampaignCommandValidator_01 | A | JobId is Empty -> Error
        [Fact]
        public void Validate_EmptyJobId_ShouldHaveError()
        {
            var command = new UpdateEmailCampaignCommand { JobId = "" };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.JobId);

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailCampaignCommandValidator",
                TestCaseID = "UpdateEmailCampaignCommandValidator_01",
                Description = "Empty JobId blocked immediately",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "JobId = empty" }
            });
        }

        // UpdateEmailCampaignCommandValidator_02 | A | Provided Subject > 255 chars -> Error
        [Fact]
        public void Validate_LongSubject_ShouldHaveError()
        {
            var command = new UpdateEmailCampaignCommand { JobId = "123", Subject = new string('A', 256) };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Subject);

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailCampaignCommandValidator",
                TestCaseID = "UpdateEmailCampaignCommandValidator_02",
                Description = "Subject string truncated safely on exceeding 255 length bounds",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Subject > 255 length" }
            });
        }

        // UpdateEmailCampaignCommandValidator_03 | A | TargetGroup = None without SpecificEmails -> Error
        [Fact]
        public void Validate_NoneTargetWithoutEmails_ShouldHaveError()
        {
            var command = new UpdateEmailCampaignCommand { JobId = "123", TargetGroup = UserTargetGroup.None, SpecificEmails = new List<string>() };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x)
                  .WithErrorMessage("Nếu TargetGroup = None thì phải có SpecificEmails.");

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailCampaignCommandValidator",
                TestCaseID = "UpdateEmailCampaignCommandValidator_03",
                Description = "Validates combination constraint avoiding ghosts (None + Empty list)",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TargetGroup = None & Emails = empty" }
            });
        }

        // UpdateEmailCampaignCommandValidator_04 | A | SpecificEmails Array contains invalid formats -> Error
        [Fact]
        public void Validate_InvalidEmailInList_ShouldHaveError()
        {
            var command = new UpdateEmailCampaignCommand { JobId = "123", TargetGroup = UserTargetGroup.None, SpecificEmails = new List<string> { "valid@abc.com", "not-an-email" } };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.SpecificEmails)
                  .WithErrorMessage("Danh sách email chứa địa chỉ không hợp lệ");

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailCampaignCommandValidator",
                TestCaseID = "UpdateEmailCampaignCommandValidator_04",
                Description = "Validates MailAddress standard internally on array contents",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Contains malformed string" }
            });
        }

        // UpdateEmailCampaignCommandValidator_05 | A | Status updated to non-Deleted (e.g. Pending) -> Error
        [Fact]
        public void Validate_InvalidStatusUpdate_ShouldHaveError()
        {
            var command = new UpdateEmailCampaignCommand { JobId = "123", Status = EmailJobStatus.Sent };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Status)
                  .WithErrorMessage("Chỉ cho phép cập nhật Status sang Deleted (xóa mềm).");

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailCampaignCommandValidator",
                TestCaseID = "UpdateEmailCampaignCommandValidator_05",
                Description = "Prevents modifying internal lifecycle transitions strictly. Only cancellation via Deleted is allowed",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status != Deleted" }
            });
        }

        // UpdateEmailCampaignCommandValidator_06 | N | Valid full update command -> Pass
        [Fact]
        public void Validate_ValidPayload_ShouldNotHaveError()
        {
            var command = new UpdateEmailCampaignCommand 
            { 
                JobId = "job-id-123",
                Subject = "New Alert",
                Body = "Updated content",
                Status = EmailJobStatus.Deleted,
                ScheduledTime = DateTime.UtcNow.AddHours(8) // Greater than offset 7
            };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Email - Update Campaign", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailCampaignCommandValidator",
                TestCaseID = "UpdateEmailCampaignCommandValidator_06",
                Description = "Satisfies all specific condition blocks safely",
                ExpectedResult = "No errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "All valid inputs" }
            });
        }
    }
}
