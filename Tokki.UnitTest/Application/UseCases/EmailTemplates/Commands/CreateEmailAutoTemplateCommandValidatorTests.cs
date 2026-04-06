using FluentAssertions;
using FluentValidation.TestHelper;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate; // The actual namespace
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.EmailTemplates.Commands
{
    public class CreateEmailAutoTemplateCommandValidatorTests
    {
        private readonly CreateEmailAutoTemplateCommandValidator _validator;

        public CreateEmailAutoTemplateCommandValidatorTests()
        {
            _validator = new CreateEmailAutoTemplateCommandValidator();
        }

        // TC-EMT-CEAV-01 | A | TemplateName Empty -> Error
        [Fact]
        public void Validate_EmptyTemplateName_ShouldHaveError()
        {
            var command = new CreateEmailAutoTemplateCommand { TemplateName = "", Type = EmailTemplateType.OfflineReminder, Value = 1, TargetGroup = UserTargetGroup.All, Subject = "S", Body = "B" };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.TemplateName);

            QACollector.LogTestCase("EmailTemplate - AutoTemplate", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailAutoTemplateCommandValidator",
                TestCaseID = "TC-EMT-CEAV-01",
                Description = "Empty template name rejected immediately",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TemplateName is empty" }
            });
        }

        // TC-EMT-CEAV-02 | A | Value <= 0 -> Error
        [Fact]
        public void Validate_ValueZeroOrLess_ShouldHaveError()
        {
            var command = new CreateEmailAutoTemplateCommand { TemplateName = "N", Type = EmailTemplateType.OfflineReminder, Value = 0, TargetGroup = UserTargetGroup.All, Subject = "S", Body = "B" };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Value);

            QACollector.LogTestCase("EmailTemplate - AutoTemplate", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailAutoTemplateCommandValidator",
                TestCaseID = "TC-EMT-CEAV-02",
                Description = "Day value boundaries block zero and negative metrics",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Value <= 0" }
            });
        }

        // TC-EMT-CEAV-03 | A | Description > 500 chars -> Error
        [Fact]
        public void Validate_LongDescription_ShouldHaveError()
        {
            var command = new CreateEmailAutoTemplateCommand { TemplateName = "N", Type = EmailTemplateType.OfflineReminder, Value = 1, TargetGroup = UserTargetGroup.All, Subject = "S", Body = "B", Description = new string('A', 501) };
            var result = _validator.TestValidate(command);
            
            result.ShouldHaveValidationErrorFor(x => x.Description);

            QACollector.LogTestCase("EmailTemplate - AutoTemplate", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailAutoTemplateCommandValidator",
                TestCaseID = "TC-EMT-CEAV-03",
                Description = "Length bounds on Description capped correctly",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Description > 500 length" }
            });
        }

        // TC-EMT-CEAV-04 | A | VipExpiringReminder missing TargetGroup rule
        [Fact]
        public void Validate_VipReminderContextIssue_ShouldHaveError()
        {
            var command = new CreateEmailAutoTemplateCommand 
            { 
                TemplateName = "N", 
                Type = EmailTemplateType.VipExpiringReminder, 
                Value = 1, 
                TargetGroup = UserTargetGroup.FreeUsers, 
                Subject = "S", 
                Body = "B" 
            };
            var result = _validator.TestValidate(command);
            
            // Testing class level rule
            result.ShouldHaveValidationErrorFor(x => x)
                  .WithErrorMessage("Template 'VIP sắp hết hạn' chỉ hợp lệ với nhóm All hoặc VipUsers.");

            QACollector.LogTestCase("EmailTemplate - AutoTemplate", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailAutoTemplateCommandValidator",
                TestCaseID = "TC-EMT-CEAV-04",
                Description = "Prevents logically assigning VIP templates to strict irregular groups like Inactive",
                ExpectedResult = "Validation Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VipExpiring && TargetGroup == Inactive" }
            });
        }

        // TC-EMT-CEAV-05 | N | Valid VipExpiringReminder Context
        [Fact]
        public void Validate_VipReminderContextValid_ShouldNotHaveError()
        {
            var command = new CreateEmailAutoTemplateCommand 
            { 
                TemplateName = "N", 
                Type = EmailTemplateType.VipExpiringReminder, 
                Value = 1, 
                TargetGroup = UserTargetGroup.VipUsers, 
                Subject = "S", 
                Body = "B" 
            };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("EmailTemplate - AutoTemplate", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailAutoTemplateCommandValidator",
                TestCaseID = "TC-EMT-CEAV-05",
                Description = "Cross property checks pass correctly aligning VIP group with context type",
                ExpectedResult = "No Errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "VipExpiring && TargetGroup == VIP" }
            });
        }

        // TC-EMT-CEAV-06 | N | Completely Valid Any Template
        [Fact]
        public void Validate_ValidBasicTemplate_ShouldNotHaveError()
        {
            var command = new CreateEmailAutoTemplateCommand 
            { 
                TemplateName = "Promo", 
                Type = EmailTemplateType.OfflineReminder, 
                Value = 10, 
                TargetGroup = UserTargetGroup.All, 
                Subject = "Come Back", 
                Body = "Check these out!" 
            };
            var result = _validator.TestValidate(command);
            
            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("EmailTemplate - AutoTemplate", new TestCaseDetail
            {
                FunctionGroup = "CreateEmailAutoTemplateCommandValidator",
                TestCaseID = "TC-EMT-CEAV-06",
                Description = "General promotion completely circumvents context blocks",
                ExpectedResult = "No Errors",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Standard input perfectly satisfied" }
            });
        }
    }
}
