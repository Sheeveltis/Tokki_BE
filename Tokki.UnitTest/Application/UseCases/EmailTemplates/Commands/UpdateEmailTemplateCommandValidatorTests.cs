using FluentAssertions;
using FluentValidation.TestHelper;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates.Commands
{
    public class UpdateEmailTemplateCommandValidatorTests
    {
        private readonly UpdateEmailTemplateCommandValidator _validator;

        public UpdateEmailTemplateCommandValidatorTests()
        {
            _validator = new UpdateEmailTemplateCommandValidator();
        }

        // UpdateEmailTemplateCommandValidator_01 | A | TemplateId Empty -> Error
        [Fact]
        public void Validate_EmptyTemplateId_ShouldHaveError()
        {
            var command = new UpdateEmailAutoTemplateCommand { TemplateId = "" };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.TemplateId);

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailTemplateCommandValidator",
                TestCaseID = "UpdateEmailTemplateCommandValidator_01",
                Description = "Empty TemplateId rejected immediately",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TemplateId is empty" }
            });
        }

        // UpdateEmailTemplateCommandValidator_02 | A | Value <= 0 -> Error
        [Fact]
        public void Validate_ValueZeroOrLess_ShouldHaveError()
        {
            var command = new UpdateEmailAutoTemplateCommand { TemplateId = "T1", Value = 0 };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Value);

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailTemplateCommandValidator",
                TestCaseID = "UpdateEmailTemplateCommandValidator_02",
                Description = "Optional Value field restricts 0 inputs",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Value <= 0" }
            });
        }

        // UpdateEmailTemplateCommandValidator_03 | A | Description > 500 chars -> Error
        [Fact]
        public void Validate_LongDescription_ShouldHaveError()
        {
            var command = new UpdateEmailAutoTemplateCommand { TemplateId = "T1", Description = new string('A', 501) };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Description);

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailTemplateCommandValidator",
                TestCaseID = "UpdateEmailTemplateCommandValidator_03",
                Description = "Description length restricted safely",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Description > 500 length" }
            });
        }

        // UpdateEmailTemplateCommandValidator_04 | A | VipExpiringReminder missing TargetGroup rule
        [Fact]
        public void Validate_VipReminderContextIssue_ShouldHaveError()
        {
            var command = new UpdateEmailAutoTemplateCommand 
            { 
                TemplateId = "T1",
                Type = EmailTemplateType.VipExpiringReminder, 
                TargetGroup = UserTargetGroup.FreeUsers
            };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x)
                  .WithErrorMessage("Template 'VIP sắp hết hạn' chỉ hợp lệ với nhóm All hoặc VipUsers.");

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailTemplateCommandValidator",
                TestCaseID = "UpdateEmailTemplateCommandValidator_04",
                Description = "Cross property checks block irregular TargetGroups on VIP types",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VipExpiring && TargetGroup == Inactive" }
            });
        }

        // UpdateEmailTemplateCommandValidator_05 | N | Valid VipExpiringReminder Context
        [Fact]
        public void Validate_VipReminderContextValid_ShouldNotHaveError()
        {
            var command = new UpdateEmailAutoTemplateCommand 
            { 
                TemplateId = "T1",
                Type = EmailTemplateType.VipExpiringReminder,
                TargetGroup = UserTargetGroup.VipUsers
            };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailTemplateCommandValidator",
                TestCaseID = "UpdateEmailTemplateCommandValidator_05",
                Description = "Cross property checks pass correctly aligning VIP group with context type on update",
                ExpectedResult = "No Errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VipExpiring && TargetGroup == VIP" }
            });
        }

        // UpdateEmailTemplateCommandValidator_06 | N | Completely Valid Sparse Template Update
        [Fact]
        public void Validate_ValidBasicTemplate_ShouldNotHaveError()
        {
            var command = new UpdateEmailAutoTemplateCommand 
            { 
                TemplateId = "T-123",
                Subject = "Come Back!"
                // Rest of properties are ignored when null via .When() extensions correctly
            };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Email Template - Update Auto", new TestCaseDetail
            {
                FunctionGroup = "UpdateEmailTemplateCommandValidator",
                TestCaseID = "UpdateEmailTemplateCommandValidator_06",
                Description = "Sparse updates validate only specified properties cleanly",
                ExpectedResult = "No Errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Only TemplateId and Subject provided" }
            });
        }
    }
}
