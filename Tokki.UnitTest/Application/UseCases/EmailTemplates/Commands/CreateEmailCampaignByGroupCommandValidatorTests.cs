using FluentAssertions;
using FluentValidation.TestHelper;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailCampaign;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates.Commands
{
    public class CreateEmailCampaignByGroupCommandValidatorTests
    {
        private readonly CreateEmailCampaignByGroupCommandValidator _validator;

        public CreateEmailCampaignByGroupCommandValidatorTests()
        {
            _validator = new CreateEmailCampaignByGroupCommandValidator();
        }

        // TC-EMT-CECV-01 | A | Subject Empty -> Error
        [Fact]
        public void Validate_EmptySubject_ShouldHaveError()
        {
            var command = new CreateEmailCampaignByGroupCommand { Subject = "", Body = "B", TargetGroup = UserTargetGroup.All };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Subject);

            QACollector.LogTestCase("EmailTemplate - Campaign", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailCampaignByGroupCommandValidator",
                TestCaseID = "TC-EMT-CECV-01",
                Description = "Empty subject fails immediately",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Subject is empty string" }
            });
        }

        // TC-EMT-CECV-02 | A | Body Empty -> Error
        [Fact]
        public void Validate_EmptyBody_ShouldHaveError()
        {
            var command = new CreateEmailCampaignByGroupCommand { Subject = "S", Body = "  ", TargetGroup = UserTargetGroup.All };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Body);

            QACollector.LogTestCase("EmailTemplate - Campaign", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailCampaignByGroupCommandValidator",
                TestCaseID = "TC-EMT-CECV-02",
                Description = "Empty body content prohibited",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Body contains only whitespace" }
            });
        }

        // TC-EMT-CECV-03 | A | ScheduledTime in the past -> Error
        [Fact]
        public void Validate_PastScheduledTime_ShouldHaveError()
        {
            var past = DateTimeOffset.UtcNow.AddDays(-1);
            var command = new CreateEmailCampaignByGroupCommand { Subject = "S", Body = "B", ScheduledTime = past };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.ScheduledTime)
                  .WithErrorMessage("Thời gian lên lịch gửi phải lớn hơn thời gian hiện tại (giờ Việt Nam).");

            QACollector.LogTestCase("EmailTemplate - Campaign", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailCampaignByGroupCommandValidator",
                TestCaseID = "TC-EMT-CECV-03",
                Description = "Time machine prevention correctly checks schedule times are future relative",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Scheduled time - 1 day" }
            });
        }

        // TC-EMT-CECV-04 | A | SpecificEmails Contains Invalid Emails
        [Fact]
        public void Validate_InvalidEmailList_ShouldHaveError()
        {
            var command = new CreateEmailCampaignByGroupCommand 
            { 
                Subject = "S", 
                Body = "B", 
                SpecificEmails = new List<string> { "valid@abc.com", "invalid-email" }
            };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.SpecificEmails)
                  .WithErrorMessage("Danh sách email chứa địa chỉ không hợp lệ");

            QACollector.LogTestCase("EmailTemplate - Campaign", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailCampaignByGroupCommandValidator",
                TestCaseID = "TC-EMT-CECV-04",
                Description = "Email lists are strictly parsed via System.Net.Mail structure checks",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Collection contains malformed address" }
            });
        }

        // TC-EMT-CECV-05 | N | Perfectly Valid Payload -> Pass
        [Fact]
        public void Validate_ValidPayload_ShouldNotHaveError()
        {
            var future = DateTimeOffset.UtcNow.AddDays(7);
            var command = new CreateEmailCampaignByGroupCommand 
            { 
                Subject = "Huge Sale!", 
                Body = "Content",
                TargetGroup = UserTargetGroup.VipUsers,
                ScheduledTime = future,
                SpecificEmails = new List<string> { "user@abc.com" }
            };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("EmailTemplate - Campaign", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailCampaignByGroupCommandValidator",
                TestCaseID = "TC-EMT-CECV-05",
                Description = "All boundaries completely satisfied",
                ExpectedResult = "No Errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Scheduled future, valid arrays, populated strings" }
            });
        }

        // TC-EMT-CECV-06 | N | Null SpecificEmails and Null ScheduledTime -> Pass
        [Fact]
        public void Validate_NullOptionals_ShouldNotHaveError()
        {
            var command = new CreateEmailCampaignByGroupCommand 
            { 
                Subject = "Alert", 
                Body = "Body content",
                TargetGroup = UserTargetGroup.All
            };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("EmailTemplate - Campaign", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailCampaignByGroupCommandValidator",
                TestCaseID = "TC-EMT-CECV-06",
                Description = "Missing optional rules triggers no cascading verification rules",
                ExpectedResult = "No Errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Optionals ignored safely" }
            });
        }
        // TC-EMT-CECV-07 | A | Invalid TargetGroup Enum -> Error
        [Fact]
        public void Validate_InvalidTargetGroup_ShouldHaveError()
        {
            var command = new CreateEmailCampaignByGroupCommand 
            { 
                Subject = "S", 
                Body = "B",
                TargetGroup = (UserTargetGroup)999 
            };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.TargetGroup);

            QACollector.LogTestCase("EmailTemplate - Campaign", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailCampaignByGroupCommandValidator",
                TestCaseID = "TC-EMT-CECV-07",
                Description = "Invalid enum value for TargetGroup causes error",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TargetGroup is not in enum" }
            });
        }

        // TC-EMT-CECV-08 | N | Empty SpecificEmails list -> Pass
        [Fact]
        public void Validate_EmptySpecificEmails_ShouldNotHaveError()
        {
            var command = new CreateEmailCampaignByGroupCommand 
            { 
                Subject = "Alert", 
                Body = "Body content",
                TargetGroup = UserTargetGroup.All,
                SpecificEmails = new List<string>()
            };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("EmailTemplate - Campaign", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailCampaignByGroupCommandValidator",
                TestCaseID = "TC-EMT-CECV-08",
                Description = "Empty specific emails list does not trigger Must rule",
                ExpectedResult = "No Errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "SpecificEmails is empty" }
            });
        }
    }
}
