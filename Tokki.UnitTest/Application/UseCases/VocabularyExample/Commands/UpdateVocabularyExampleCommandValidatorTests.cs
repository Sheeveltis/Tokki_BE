using FluentValidation.TestHelper;
using Moq;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.VocabularyExample.Commands.UpdateExample;
using Tokki.Application.UseCases.VocabularyExample.DTOs;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.VocabularyExample.Commands
{
    public class UpdateVocabularyExampleCommandValidatorTests
    {
        private readonly UpdateVocabularyExampleCommandValidator _validator;

        public UpdateVocabularyExampleCommandValidatorTests()
        {
            _validator = new UpdateVocabularyExampleCommandValidator();
        }

        [Fact]
        public void Validate_EmptyExampleId_ShouldHaveError()
        {
            var command = new UpdateVocabularyExampleCommand { ExampleId = "", UpdateData = new VocabularyExampleUpdateDto() };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.ExampleId)
                  .WithErrorMessage("'ExampleId' không được bỏ trống.");

            QACollector.LogTestCase("Vocabulary Example - Update Validator", new TestCaseDetail
            {
                FunctionGroup     = "UpdateVocabularyExampleCommandValidator",
                TestCaseID        = "TC-VEX-UVEV-01",
                Description       = "ExampleId empty",
                ExpectedResult    = "Error ExampleId must not be empty",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ExampleId empty" }
            });
        }

        [Fact]
        public void Validate_NullUpdateData_ShouldHaveError()
        {
            var command = new UpdateVocabularyExampleCommand { ExampleId = "id1", UpdateData = null };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UpdateData)
                  .WithErrorMessage("'UpdateData' không được bỏ trống.");

            QACollector.LogTestCase("Vocabulary Example - Update Validator", new TestCaseDetail
            {
                FunctionGroup     = "UpdateVocabularyExampleCommandValidator",
                TestCaseID        = "TC-VEX-UVEV-02",
                Description       = "UpdateData null",
                ExpectedResult    = "Error UpdateData missing",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UpdateData missing" }
            });
        }

        [Fact]
        public void Validate_InvalidStatus_ShouldHaveError()
        {
            var command = new UpdateVocabularyExampleCommand 
            { 
                ExampleId = "id1", 
                UpdateData = new VocabularyExampleUpdateDto { Status = (VocabularyExampleStatus)999 } 
            };
            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("UpdateData.Status")
                  .WithErrorMessage("'Status' có một phạm vi các giá trị mà không bao gồm '999'.");

            QACollector.LogTestCase("Vocabulary Example - Update Validator", new TestCaseDetail
            {
                FunctionGroup     = "UpdateVocabularyExampleCommandValidator",
                TestCaseID        = "TC-VEX-UVEV-03",
                Description       = "Invalid enum status",
                ExpectedResult    = "Error status not in enum",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid status" }
            });
        }

        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveError()
        {
            var command = new UpdateVocabularyExampleCommand 
            { 
                ExampleId = "id1", 
                UpdateData = new VocabularyExampleUpdateDto { Status = VocabularyExampleStatus.Active } 
            };
            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("Vocabulary Example - Update Validator", new TestCaseDetail
            {
                FunctionGroup     = "UpdateVocabularyExampleCommandValidator",
                TestCaseID        = "TC-VEX-UVEV-04",
                Description       = "Valid inputs check",
                ExpectedResult    = "No errors",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid parameters" }
            });
        }
    }
}
